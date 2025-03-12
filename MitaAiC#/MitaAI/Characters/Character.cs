using MelonLoader;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace MitaAI
{

    [RegisterTypeInIl2Cpp]
    public class Character : MonoBehaviour
    {
        public MitaCore.character character;
        public bool isCartdige;
        public int PointsOrder = 0;

        public void init(MitaCore.character character)
        {
            this.character = character;
            CharacterControl.Characters.Add(this);
        }
        public void init_cartridge()
        {

            this.isCartdige = true;
            init(CharacterControl.get_cart());

        }

        public void increaseOrderPoints(int n = 1)
        {
            PointsOrder += n;
        }
    }
}
