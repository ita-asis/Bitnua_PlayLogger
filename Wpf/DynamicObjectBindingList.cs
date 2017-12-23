using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Dynamic;
using System.Collections.ObjectModel;


namespace PlayLogger.Wpf
{
    [Serializable()]
    public class DynamicObjectBindingList : ObservableCollection<ExpandoObject>, ITypedList
    {
        [NonSerialized()]
        private Type m_elementType;

        public DynamicObjectBindingList(Type elementType = null)
            : base()
        {
            m_elementType = elementType;
        }

        public DynamicObjectBindingList(IList<ExpandoObject> data, Type elementType = null)
            : base(data)
        {
            m_elementType = elementType;
        }
        #region ITypedList Implementation

        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            PropertyDescriptorCollection pdc = null;

            if (this.Any())
            {
                var props = Dynamitey.Dynamic.GetMemberNames(this.First()).Select(name => new DynamicPropertyDescriptor(name, getAttrs(name))).ToArray();
                pdc = new PropertyDescriptorCollection(props);
            }

            return pdc;
        }

        private Attribute[] getAttrs(string propName)
        {
            Attribute[] res = null;
            if (m_elementType != null)
            {
                var prop = m_elementType.GetProperty(propName);
                if (prop != null)
                {
                    res = prop.GetCustomAttributes(true).Cast<Attribute>().ToArray();
                }
            }

            return res;
        }

        public string GetListName(PropertyDescriptor[] listAccessors)
        {
            return null;
        }

        #endregion
    }
}
