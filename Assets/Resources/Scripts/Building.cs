using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Resources.Scripts
{
    /// <summary>
    /// Производственное здание: производит ресурс, хранит amount, предоставляет API для подхода игрока и сохранения состояния.
    /// </summary>
    public class Building : MonoBehaviour
    {
        [Header("Идентификатор для сохранения")]
        [Tooltip("Уникальный id здания для сохранения/загрузки. Если пуст — используется gameObject.name.")]
        public string buildingId;

        [Header("Ресурс")]
        [Tooltip("Название ресурса, которое будет отображаться и использоваться как ключ в ResourceManager.")]
        public string resourceName = "Железо";

        [SerializeField]
        [Tooltip("Текущее количество хранящегося ресурса в здании.")]
        private int storedAmount;

        [Tooltip("Максимальная вместимость хранения.")]
        public int storageCapacity = 100;

        [Tooltip("Интервал производства в секундах.")]
        public float produceInterval = 2f;

        [Tooltip("Сколько единиц производит здание за один тик.")]
        public int produceAmountPerTick = 5;

        [Header("Подход")]
        [Tooltip("Расстояние, на котором игрок остановится перед зданием")]
        public float approachDistance = 1.5f;

        [Header("UI")]
        public Text worldText; // если используете TextMeshPro - замените тип на TMPro.TMP_Text

        private Coroutine _productionCoroutine;

        private void Start()
        {
            UpdateWorldText();
            _productionCoroutine = StartCoroutine(ProduceRoutine());
        }

        private IEnumerator ProduceRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(produceInterval);
                storedAmount = Mathf.Min(storageCapacity, storedAmount + produceAmountPerTick);
                UpdateWorldText();
            }
        }

        private void UpdateWorldText()
        {
            if (worldText != null)
                worldText.text = $"{resourceName}\n{storedAmount}";
        }

        /// <summary>
        /// Сбор всех ресурсов из здания (игроком). Возвращает собранное количество.
        /// </summary>
        public int CollectAll()
        {
            var amount = storedAmount;
            storedAmount = 0;
            UpdateWorldText();
            return amount;
        }

        private void OnDestroy()
        {
            if (_productionCoroutine != null) StopCoroutine(_productionCoroutine);
        }

        /// <summary>
        /// Рассчитывает точку подхода перед зданием на основе clickPoint и расстояния.
        /// </summary>
        public Vector3 GetApproachPoint(Vector3 clickPoint, float distance)
        {
            Vector3 center = transform.position;
            Vector3 dir = clickPoint - center;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = transform.forward;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            }
            dir.Normalize();

            float offset = distance;
            var col = GetComponent<Collider>();
            if (col != null)
            {
                offset += col.bounds.extents.magnitude;
            }

            Vector3 point = center + dir * offset;
            point.y = center.y;
            return point;
        }

        // --- API для SaveSystem и внешних систем ---

        /// <summary>
        /// Идентификатор здания для сохранения. Если не задан — используется имя объекта.
        /// </summary>
        public string BuildingIdForSave => string.IsNullOrEmpty(buildingId) ? gameObject.name : buildingId;

        /// <summary>
        /// Получить текущее хранимое количество.
        /// </summary>
        public int GetStoredAmount() => storedAmount;

        /// <summary>
        /// Установить текущее хранимое количество (используется при загрузке).
        /// </summary>
        public void SetStoredAmount(int amount)
        {
            storedAmount = Mathf.Clamp(amount, 0, storageCapacity);
            UpdateWorldText();
        }
    }
}
