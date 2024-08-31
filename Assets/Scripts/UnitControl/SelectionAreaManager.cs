using UnityEngine;

public class SelectionAreaManager : MonoBehaviour
{
    public static SelectionAreaManager Instance;
    [SerializeField] public Transform SelectionArea;

    private void Awake()
    {
        Instance = this;
    }
}