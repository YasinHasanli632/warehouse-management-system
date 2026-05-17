using Anbar.Entities.Common;
using Anbar.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // YENI:
    // Qaimə/idxal dynamic field dəyərlərini saxlayır.
    // Məsələn:
    // FieldKey = customs_declaration_no
    // Value = AB123456
    public class InvoiceDynamicFieldValue : BaseEntity
    {
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public string FieldKey { get; set; } = null!;
        public string FieldName { get; set; } = null!;

        public FieldDataType FieldType { get; set; } = FieldDataType.Text;

        public string? Value { get; set; }

        public string? Note { get; set; }
    }
}
