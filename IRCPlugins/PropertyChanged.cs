using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCPlugin
{
    public delegate void PropertyChangedHandler(object sender, PropertyChangedArgs e);

    [Serializable]
    public class PropertyChangedArgs : EventArgs
    {
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.PropertyChangedEventArgs
        //     class.
        //
        // Parameters:
        //   propertyName:
        //     The name of the property that changed.
        public PropertyChangedArgs(string propertyName)
        {
            PropertyName = propertyName;
        }

        // Summary:
        //     Gets the name of the property that changed.
        //
        // Returns:
        //     The name of the property that changed.
        public virtual string PropertyName { get; private set; }
    }
}
