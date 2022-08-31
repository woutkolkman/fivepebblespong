using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    interface IFPGame //TODO make static class which can be inherited?
    {
        void Destruct();
        void Update(SSOracleBehavior self);
        void Draw(SSOracleBehavior self);

        //Edge coordinates:
        //TR 740,600 (roughly tile38,30)
        //TL 240,600 (roughly tile10,30)
        //BR 740,100 (roughly tile38,4)
        //BL 240,100 (roughly tile10,4)
    }
}
