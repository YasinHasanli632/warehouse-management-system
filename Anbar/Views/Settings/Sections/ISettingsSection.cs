using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anbar.Views.Settings.Sections
{
    public interface ISettingsSection
    {
        void Bind();
        void ApplyChanges();
    }
}
