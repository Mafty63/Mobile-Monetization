using System;

namespace MobileCore.DefineSystem
{
    /// <summary>
    /// Attribute untuk mendeklarasikan define symbol yang diperlukan oleh class Manager.
    /// Sistem akan otomatis menambahkan define symbol jika type yang dideklarasikan tersedia.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DefineAttribute : Attribute
    {
        /// <summary>
        /// Define symbol yang akan ditambahkan ke Player Settings
        /// </summary>
        public string DefineSymbol { get; }

        /// <summary>
        /// Type name lengkap yang digunakan untuk mengecek apakah SDK tersedia
        /// Contoh: "GoogleMobileAds.Api.MobileAds"
        /// </summary>
        public string TypeCheck { get; }

        /// <summary>
        /// Deskripsi opsional untuk dokumentasi
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Constructor untuk DefineAttribute
        /// </summary>
        /// <param name="defineSymbol">Define symbol yang akan ditambahkan (contoh: "ADMOB_PROVIDER")</param>
        /// <param name="typeCheck">Type name lengkap untuk mengecek ketersediaan SDK (contoh: "GoogleMobileAds.Api.MobileAds")</param>
        /// <param name="description">Deskripsi opsional</param>
        public DefineAttribute(string defineSymbol, string typeCheck, string description = "")
        {
            DefineSymbol = defineSymbol;
            TypeCheck = typeCheck;
            Description = description;
        }
    }
}

