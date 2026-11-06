using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel.UniKit
{
    public class InfiniteScrollRectItemView : MonoBehaviour
    {
        public Text NameText;

        public void Refresh(string name)
        {
            this.NameText.text = name;
        }
    }
}