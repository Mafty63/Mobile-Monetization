using System;

namespace MobileCore.MainModule
{
    /// <summary>
    /// Attribute untuk mendeklarasikan bahwa class ini adalah module yang dapat ditambahkan ke MainSystemSettings.
    /// Module dengan attribute ini akan muncul di dropdown "Add Module".
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ModuleAttribute : Attribute
    {
        /// <summary>
        /// Nama yang ditampilkan di dropdown
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Deskripsi module (opsional)
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Urutan tampilan di dropdown (semakin kecil, semakin atas)
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Constructor untuk ModuleAttribute
        /// </summary>
        /// <param name="displayName">Nama yang ditampilkan di dropdown</param>
        /// <param name="description">Deskripsi module (opsional)</param>
        /// <param name="order">Urutan tampilan di dropdown (default: 0)</param>
        public ModuleAttribute(string displayName, string description = "", int order = 0)
        {
            DisplayName = displayName;
            Description = description;
            Order = order;
        }
    }
}

