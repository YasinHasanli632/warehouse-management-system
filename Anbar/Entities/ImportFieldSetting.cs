using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // İdxal qaiməsində hansı sahələrin görünüb/gizlənəcəyini saxlayır.
    public class ImportFieldSetting : BaseEntity
    {
        public string FieldKey { get; set; } = null!;
        public string DisplayName { get; set; } = null!;

        // YENI:
        // Sahənin tipi: Text, Number, Date, Dropdown və s.
        public FieldDataType FieldType { get; set; } = FieldDataType.Text;

        public bool IsVisible { get; set; } = true;
        public bool IsRequired { get; set; } = false;

        // YENI:
        // Bu sahə qaimədə göstərilsinmi.
        public bool ShowOnInvoice { get; set; } = true;

        // YENI:
        // Dropdown dəyərləri JSON və ya vergüllə ayrılmış formada saxlanıla bilər.
        public string? OptionsJson { get; set; }

        // YENI:
        // Default dəyər.
        public string? DefaultValue { get; set; }

        // YENI:
        // Placeholder.
        public string? Placeholder { get; set; }

        public int SortOrder { get; set; } = 0;
    }
}
