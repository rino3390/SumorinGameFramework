using Rino.GameFramework.Core.DDDCore.Domain;

namespace Game.Domain.Sample
{
    /// <summary>
    /// 範例 Entity，展示 DDD Entity 的基本實作
    /// </summary>
    public class SampleEntity : Entity
    {
        public string Name { get; private set; }
        public int Value { get; private set; }

        public SampleEntity(string id, string name, int value) : base(id)
        {
            Name = name;
            Value = value;
        }

        public void UpdateName(string newName)
        {
            Name = newName;
        }

        public void UpdateValue(int newValue)
        {
            Value = newValue;
        }
    }
}
