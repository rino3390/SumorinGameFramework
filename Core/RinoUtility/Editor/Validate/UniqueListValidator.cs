#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: RegisterValidator(typeof(Rino.GameFramework.RinoUtility.Editor.UniqueListValidator<>))]

namespace Rino.GameFramework.RinoUtility.Editor
{
    /// <summary>
    /// UniqueListAttribute 的 Odin Inspector 驗證器
    /// </summary>
    /// <typeparam name="T">清單項目類型</typeparam>
    public class UniqueListValidator<T> : AttributeValidator<UniqueListAttribute, T>
    {
        private MemberInfo cachedMember;
        private bool memberLookupDone;

        /// <summary>
        /// 判斷是否可以驗證指定的屬性
        /// </summary>
        /// <param name="property">要驗證的屬性</param>
        /// <returns>如果屬性的父類型是 IList 則回傳 true</returns>
        public override bool CanValidateProperty(InspectorProperty property)
        {
            return typeof(IList).IsAssignableFrom(property.ParentType);
        }

        /// <summary>
        /// 執行驗證邏輯
        /// </summary>
        /// <param name="result">驗證結果</param>
        protected override void Validate(ValidationResult result)
        {
            if (ValueEntry.SmartValue == null)
                return;

            var list = (List<T>)Property.Parent.ValueEntry.WeakSmartValue;
            var currentValue = ValueEntry.SmartValue;
            var propertyName = Attribute.PropertyName;

            int duplicateCount;

            if (string.IsNullOrEmpty(propertyName))
            {
                duplicateCount = list.Count(x => x != null && x.Equals(currentValue));
            }
            else
            {
                var currentKeyValue = GetMemberValue(currentValue, propertyName);
                if (currentKeyValue == null)
                    return;

                duplicateCount = list.Count(x => x != null && Equals(GetMemberValue(x, propertyName), currentKeyValue));
            }

            if (duplicateCount <= 1)
                return;

            result.ResultType = ValidationResultType.Error;
            result.Message = Attribute.ErrorMessage;
        }

        private object GetMemberValue(T instance, string memberName)
        {
            if (!memberLookupDone)
            {
                var type = typeof(T);
                cachedMember = (MemberInfo)type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                               ?? type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                memberLookupDone = true;
            }

            return cachedMember switch
            {
                FieldInfo field => field.GetValue(instance),
                PropertyInfo prop => prop.GetValue(instance),
                _ => null
            };
        }
    }
}
#endif
