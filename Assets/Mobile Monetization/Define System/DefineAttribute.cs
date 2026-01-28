using System;

namespace MobileCore.DefineSystem
{
    /// <summary>
    /// Attribute to declare define symbols required by Manager classes.
    /// The system automatically adds the symbol if the declared type is available.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DefineAttribute : Attribute
    {
        public string DefineSymbol { get; }
        public string TypeCheck { get; }
        public string Description { get; }

        public DefineAttribute(string defineSymbol, string typeCheck, string description = "")
        {
            DefineSymbol = defineSymbol;
            TypeCheck = typeCheck;
            Description = description;
        }
    }
}

