using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : NetworkBehaviour // спавнер предметов юзает навмеш.
{
    [SerializeField] private GameObject _itemPrefab;

    [Space(9)]

    [SerializeField, Min(0)] private int _maxSpawnAmount = 50;
    [SerializeField, Min(0.1f)] private float _spawnRate = 4f;

    [Space(9)]

    [SerializeField] private Vector3 _bounds = new Vector3(1, 1, 1);

    [ServerCallback] // этот атрибут запрещает вызывать метод всем клиентам кроме сервера
    public void StartSpawnProcces()
    {
        InvokeRepeating(nameof(SpawnItem), 0f, _spawnRate); // постояно вызывает SpawnItem
    }

    [ServerCallback] // этот атрибут запрещает вызывать метод всем клиентам кроме сервера
    public void StopSpawnProcces()
    {
        CancelInvoke(nameof(SpawnItem)); // отменяет постоянный вызов метода
    }

    [ServerCallback] // этот атрибут запрещает вызывать метод всем клиентам кроме сервера
    public void DestroyAll()
    {
        PickableItem[] all = FindObjectsOfType<PickableItem>(true);

        foreach (var item in all)
        {
            NetworkServer.Destroy(item.gameObject);
        }
    }

    public void SpawnItem()
    {
        PickableItem[] allSpawned = FindObjectsOfType<PickableItem>();

        if (allSpawned.Length >= _maxSpawnAmount) return; // тут же выходит их метода если мы достигли лимита

        Vector3 randomPlace = new Vector3
        (
            Random.Range(-(_bounds.x / 2), _bounds.x / 2),
            Random.Range(-(_bounds.y / 2), _bounds.y / 2),
            Random.Range(-(_bounds.z / 2), _bounds.z / 2)
        );

        NavMeshHit hit;
        NavMesh.SamplePosition(randomPlace, out hit, randomPlace.magnitude, 1);

        GameObject spawnedItem = Instantiate(_itemPrefab, hit.position + (Vector3.up * 1.5f), Quaternion.identity);

        NetworkServer.Spawn(spawnedItem);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = ColorISH.Cyan;
        Gizmos.DrawWireCube(Vector3.zero, _bounds);
    }
}
