using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitJson;
using UnityEngine;
namespace FromJianghuENMod
{
    [Serializable]
    public class ModSettings
    {
        public bool enableDebugLog;
        public List<Patcher> patchers;
    }
  
}
