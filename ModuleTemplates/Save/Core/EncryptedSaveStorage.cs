using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Sumorin.Save
{
	/// <summary>
	///     存取媒介的加密包裝，把資料值加密後才交給內層落地
	/// </summary>
	/// <remarks>
	///     內層是任何一個 <see cref="ISaveStorage" />，本類別不在意資料實際存到哪裡。
	///     加密只作用在資料值，存檔鍵與中繼資料原樣交給內層，存檔清單因此仍可讀。
	///     每筆值各自加密，格式為初始向量、密文、驗證碼三段接起來後轉 Base64。
	///     採先加密再算驗證碼，讀取時先驗證再解密，驗證不過就不解密。
	///     任何一筆值驗證不過或解不開，整份存檔視為讀不到，回傳 null。
	/// </remarks>
	// ponytail: 金鑰隨程式發佈，擋的是玩家改存檔，擋不了拆執行檔挖金鑰的人。真要防作弊得把存檔搬到伺服器
	public class EncryptedSaveStorage: ISaveStorage
	{
		private const int IvLength = 16;
		private const int MacLength = 32;

		private readonly ISaveStorage inner;
		private readonly byte[] cipherKey;
		private readonly byte[] macKey;

		/// <summary>
		///     建立加密包裝
		/// </summary>
		/// <param name="inner">實際負責落地的內層存取媒介</param>
		/// <param name="passphrase">加密通行碼，換掉通行碼會讓既有存檔無法讀取</param>
		/// <exception cref="ArgumentException">通行碼為空時拋出</exception>
		public EncryptedSaveStorage(ISaveStorage inner, string passphrase)
		{
			if(string.IsNullOrEmpty(passphrase))
			{
				throw new ArgumentException("加密通行碼不可為空", nameof(passphrase));
			}

			this.inner = inner;
			cipherKey = DeriveKey("cipher", passphrase);
			macKey = DeriveKey("mac", passphrase);
		}

	#region ISaveStorage Members
		/// <inheritdoc />
		public bool Save(SaveSlotInfo info, IReadOnlyDictionary<string, string> data)
		{
			var encrypted = new Dictionary<string, string>(data.Count);

			foreach(var pair in data)
			{
				encrypted[pair.Key] = Encrypt(pair.Value);
			}

			return inner.Save(info, encrypted);
		}

		/// <inheritdoc />
		public IReadOnlyDictionary<string, string> Load(string slotId)
		{
			var data = inner.Load(slotId);
			if(data == null) return null;

			var decrypted = new Dictionary<string, string>(data.Count);

			foreach(var pair in data)
			{
				var value = Decrypt(pair.Value);
				if(value == null) return null;

				decrypted[pair.Key] = value;
			}

			return decrypted;
		}

		/// <inheritdoc />
		public bool Delete(string slotId) => inner.Delete(slotId);

		/// <inheritdoc />
		public IReadOnlyList<SaveSlotInfo> ListSlots() => inner.ListSlots();
	#endregion

		// ponytail: 通行碼是原始碼常數不是使用者密碼，PBKDF2 的慢雜湊在這裡買不到東西
		private static byte[] DeriveKey(string label, string passphrase)
		{
			using var sha = SHA256.Create();

			return sha.ComputeHash(Encoding.UTF8.GetBytes($"{label}:{passphrase}"));
		}

		private static bool MatchesMac(byte[] record, int offset, byte[] expected)
		{
			var difference = 0;

			for(var i = 0; i < expected.Length; i++)
			{
				difference |= record[offset + i] ^ expected[i];
			}

			return difference == 0;
		}

		private string Encrypt(string plain)
		{
			using var aes = Aes.Create();
			aes.Key = cipherKey;
			aes.GenerateIV();

			using var encryptor = aes.CreateEncryptor();
			var body = Encoding.UTF8.GetBytes(plain ?? string.Empty);
			var cipher = encryptor.TransformFinalBlock(body, 0, body.Length);

			var record = new byte[IvLength + cipher.Length + MacLength];
			Buffer.BlockCopy(aes.IV, 0, record, 0, IvLength);
			Buffer.BlockCopy(cipher, 0, record, IvLength, cipher.Length);

			using var hmac = new HMACSHA256(macKey);
			Buffer.BlockCopy(hmac.ComputeHash(record, 0, IvLength + cipher.Length), 0, record, IvLength + cipher.Length, MacLength);

			return Convert.ToBase64String(record);
		}

		private string Decrypt(string record)
		{
			if(string.IsNullOrEmpty(record)) return null;

			byte[] bytes;

			try
			{
				bytes = Convert.FromBase64String(record);
			}
			catch(FormatException)
			{
				return null;
			}

			if(bytes.Length <= IvLength + MacLength) return null;

			var bodyLength = bytes.Length - MacLength;

			using var hmac = new HMACSHA256(macKey);
			if(!MatchesMac(bytes, bodyLength, hmac.ComputeHash(bytes, 0, bodyLength))) return null;

			var iv = new byte[IvLength];
			Buffer.BlockCopy(bytes, 0, iv, 0, IvLength);

			using var aes = Aes.Create();
			aes.Key = cipherKey;
			aes.IV = iv;

			using var decryptor = aes.CreateDecryptor();

			try
			{
				return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(bytes, IvLength, bodyLength - IvLength));
			}
			catch(CryptographicException)
			{
				return null;
			}
		}
	}
}