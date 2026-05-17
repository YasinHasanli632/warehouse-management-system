using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities.Enum
{
    // YENI:
    // Dynamic form sahələrinin tipini saxlayır.
    // Məsələn idxal qaiməsində Gömrük bəyannamə № = Text,
    // İdxal tarixi = Date, Məzənnə = Number.
    public enum FieldDataType
    {
        Text = 1,
        Number = 2,
        Date = 3,
        Dropdown = 4,
        Checkbox = 5,
        TextArea = 6
    }
}
