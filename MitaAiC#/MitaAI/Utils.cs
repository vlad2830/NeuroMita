using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MitaAI
{
    public static class Utils
    {
        public static void CopyComponentValues(ObjectInteractive source, ObjectInteractive destination)
        {
            if (source == null || destination == null)
            {
                MelonLogger.Msg("Source or destination is null!");
                return;
            }

            // Если нужно скопировать все поля автоматически, можно использовать рефлексию
            var fields = typeof(ObjectInteractive).GetFields();
            foreach (var field in fields)
            {
                field.SetValue(destination, field.GetValue(source));
            }

            MelonLogger.Msg("Component values copied!");
        }
    }

}
