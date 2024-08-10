using UnityEngine;

public class PlayerShootManager : MonoBehaviour
{
    [SerializeField] private GameObject _shootPopupPrefab;
    public static PlayerShootManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // private void Start()
    // {
    //     var system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerShootingSystem>();
    //
    //     system.OnShoot += PlayerShootingSystem_OnShoot;
    // }
    //
    // private void PlayerShootingSystem_OnShoot(object sender, EventArgs e)
    // {
    //     var playerEntity = (Entity)sender;
    //     var localTransform = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<LocalTransform>(playerEntity);
    //     Instantiate(_shootPopupPrefab, localTransform.Position, quaternion.identity);
    // }

    public void PlayerShoot(Vector3 playerPosition)
    {
        Instantiate(_shootPopupPrefab, playerPosition, Quaternion.identity);
    }
}