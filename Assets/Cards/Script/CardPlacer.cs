using UnityEngine;

public class CardPlacer : MonoBehaviour
{
    public static CardPlacer Instance;

    private GameObject selectedPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void SetSelectedCard(GameObject prefab)
    {
        selectedPrefab = prefab;
    }

    private void Update()
    {
        
        if (selectedPrefab != null && Input.GetMouseButtonDown(0))
        {
            
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 spawnPos = hit.point;

                // Levantar la torreta la mitad de su altura
                float height = selectedPrefab.GetComponent<Renderer>().bounds.size.y;
                spawnPos.y += height / 2f;

                Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
                selectedPrefab = null;
            }
        }
    }
}
