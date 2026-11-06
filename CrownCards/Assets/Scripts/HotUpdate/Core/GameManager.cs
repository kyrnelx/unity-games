using System;
using UnityEngine;

namespace Game.Manager
{
    public class GameManager : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}