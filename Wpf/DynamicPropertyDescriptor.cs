
using System;
using System.ComponentModel;
using Dynamitey;
namespace PlayLogger.Wpf
{

    public class DynamicPropertyDescriptor : PropertyDescriptor
    {
        public DynamicPropertyDescriptor(string name, Attribute[] attrs = null)
            : base(name,attrs)
        {
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override object GetValue(object component)
        {
            return Dynamic.InvokeGet(component, Name);
        }

        public override void ResetValue(object component)
        {

        }

        public override void SetValue(object component, object value)
        {
            Dynamic.InvokeSet(component, Name, value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get { return typeof(object); }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get
            {
                return typeof(object);
            }
        }
    }

}
