using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    [RequireComponent(typeof(Buildable))]
    [RequireComponent(typeof(Construction))]
    public class BakeryOven : MonoBehaviour
    {
        private UniqueID unique_id;

        private static List<BakeryOven> oven_list = new List<BakeryOven>();
        public static List<BakeryOven> GetAll() => oven_list;

        void Awake()
        {
            oven_list.Add(this);
            unique_id = GetComponent<UniqueID>();
        }

        void OnDestroy()
        {
            oven_list.Remove(this);
        }

        public string GetUID() => unique_id != null ? unique_id.unique_id : "";
    }
}
