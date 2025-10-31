using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Resources.Scripts
{
    public class PopupManager : MonoBehaviour
    {
        [Header("Refs")]
        public GameObject popupPanel;
        public Text popupText;
        [Tooltip("Время показа в секундах (без анимаций)")]
        public float displayTime = 1.2f;

        [Header("Анимация")]
        public float animInDuration = 0.35f;
        public float animOutDuration = 0.45f;
        public float moveDistance = 80f;

        private Coroutine _current;
        private CanvasGroup _canvasGroup;
        private RectTransform _rect;

        private void Awake()
        {
            if (popupPanel == null) return;
            _canvasGroup = popupPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = popupPanel.AddComponent<CanvasGroup>();

            _rect = popupPanel.GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }
        
        [Obsolete("Obsolete")]
        public void ShowCollected(int total)
        {
            ShowResourcesSummary();
        }
        
        [Obsolete("Obsolete")]
        private void ShowResourcesSummary()
        {
            var text = BuildResourcesPopupText();
            Show(text);
        }

        private void Show(string text)
        {
            if (popupPanel == null || popupText == null) return;
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(ShowRoutine(text));
        }
        
        [Obsolete("Obsolete")]
        private string BuildResourcesPopupText()
        {
            var sb = new StringBuilder();

            Dictionary<string, int> collected = null;
            var rm = FindObjectOfType<ResourceManager>();
            if (rm != null)
            {
                try { collected = rm.GetAllCollected(); }
                catch { collected = null; }
            }

            var produced = new Dictionary<string, int>();
            var allBuildings = FindObjectsOfType<Building>();
            foreach (var b in allBuildings)
            {
                if (b == null) continue;
                var name = string.IsNullOrEmpty(b.resourceName) ? "Unknown" : b.resourceName;
                int amt;
                try { amt = b.GetStoredAmount(); }
                catch { amt = 0; }

                if (!produced.TryAdd(name, amt)) produced[name] += amt;
            }

            sb.AppendLine("Собрано:");
            if (collected == null || collected.Count == 0)
            {
                sb.AppendLine("  — ничего не собрано");
            }
            else
            {
                foreach (var kv in SortedEntries(collected))
                {
                    sb.AppendLine($"  • {kv.Key}: {kv.Value}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Произведено (в зданиях):");
            if (produced.Count == 0)
            {
                sb.AppendLine("  — производство отсутствует");
            }
            else
            {
                foreach (var kv in SortedEntries(produced))
                {
                    sb.AppendLine($"  • {kv.Key}: {kv.Value}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private List<KeyValuePair<string,int>> SortedEntries(Dictionary<string,int> dict)
        {
            var list = new List<KeyValuePair<string,int>>(dict);
            list.Sort((a,b) =>
            {
                var cmp = string.Compare(a.Key, b.Key, StringComparison.CurrentCulture);
                return cmp != 0 ? cmp : b.Value.CompareTo(a.Value);
            });
            return list;
        }

        private IEnumerator ShowRoutine(string text)
        {
            popupText.text = text;
            popupPanel.SetActive(true);

            if (!_rect) _rect = popupPanel.GetComponent<RectTransform>();
            if (!_canvasGroup) _canvasGroup = popupPanel.GetComponent<CanvasGroup>();

            var originalPos = _rect.anchoredPosition;
            var startPos = originalPos - new Vector2(0, moveDistance * 0.6f);
            var endPos = originalPos + new Vector2(0, moveDistance);

            _rect.anchoredPosition = startPos;
            _canvasGroup.alpha = 0f;

            var t = 0f;
            while (t < animInDuration)
            {
                t += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(t / animInDuration);
                _canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, p);
                _rect.anchoredPosition = Vector2.Lerp(startPos, originalPos, Mathf.SmoothStep(0f, 1f, p));
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _rect.anchoredPosition = originalPos;

            yield return new WaitForSecondsRealtime(displayTime);

            t = 0f;
            while (t < animOutDuration)
            {
                t += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(t / animOutDuration);
                _canvasGroup.alpha = Mathf.SmoothStep(1f, 0f, p);
                _rect.anchoredPosition = Vector2.Lerp(originalPos, endPos, Mathf.SmoothStep(0f, 1f, p));
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _rect.anchoredPosition = originalPos;
            popupPanel.SetActive(false);
            _current = null;
        }
    }
}
