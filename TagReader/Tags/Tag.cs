using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagReader.Tags
{
    public interface Tag
    {
        // Info Retrevial
        String getProperty(String property);
        void writeProperty(String property, params object[] args);

        void updateTagBuffer();
        byte[] getTagBuffer();
    }
}
