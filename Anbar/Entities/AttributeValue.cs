using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity xüsusiyyətin dəyərini saxlayır.
    // Məsələn:
    // Rəng -> Ağ
    // Ölçü -> 200 sm
    // Material -> MDF
    public class AttributeValue : BaseEntity
    {
        // Hansı xüsusiyyət başlığına aid olduğunu göstərir.
        public int AttributeDefinitionId { get; set; }

        // Aid olduğu xüsusiyyət başlığı.
        public AttributeDefinition AttributeDefinition { get; set; } = null!;

        // Xüsusiyyət dəyəri.
        // Məsələn: Ağ, Qara, 200 sm, MDF.
        public string Value { get; set; } = null!;

        // Bu dəyər hansı məhsullara bağlanıbsa onları saxlayır.
        public ICollection<ProductAttribute> ProductAttributes { get; set; } = new List<ProductAttribute>();
    }
}
