using Anbar.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Entities
{
    // Bu entity məhsulla seçilmiş xüsusiyyət dəyərini birləşdirir.
    // Artıq burada Key/Value sərbəst yazı kimi saxlanmır.
    // Məhsul hazır AttributeValue ilə əlaqələndirilir.
    //
    // Məsələn:
    // Product: Qapı
    // AttributeValue: Ağ
    //
    // Product: Qapı
    // AttributeValue: 200 sm
    public class ProductAttribute : BaseEntity
    {
        public int ProductId { get; set; }

        public int AttributeDefinitionId { get; set; }

        public int AttributeValueId { get; set; }

        public Product Product { get; set; } = null!;

        public AttributeDefinition AttributeDefinition { get; set; } = null!;

        public AttributeValue AttributeValue { get; set; } = null!;
    }
}
