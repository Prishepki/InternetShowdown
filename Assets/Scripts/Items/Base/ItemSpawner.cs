using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : NetworkBehaviour // спавнер предметов юзает навмеш.
{
    [SerializeField] private GameObject _itemPrefab;

    [Space(9)]

    [SerializeField, Min(1)] private float _spawnRadius = 100f;
    [SerializeField, Min(0)] private int _maxSpawnAmount = 50;
    [SerializeField, Min(0.1f)] private float _spawnRate = 4f;

    [ServerCallback] // этот атрибут запрещает вызывать метод всем клиентам кроме сервера
    private void Start()
    {
        InvokeRepeating(nameof(SpawnItem), _spawnRate, _spawnRate); // постояно вызывает SpawnItem
    }
    
    public void SpawnItem()
    {
        PickableItem[] allSpawned = FindObjectsOfType<PickableItem>();

        if (allSpawned.Length >= _maxSpawnAmount) return; // тут же выходит их метода если мы достигли лимита

        Vector3 randomDirection = Random.insideUnitSphere * _spawnRadius;

        NavMeshHit hit;

        while (!NavMesh.SamplePosition(randomDirection, out hit, _spawnRadius, 1)) // пока рандомная точка в сфере не попадет на навмеш
        {
            randomDirection = Random.insideUnitSphere * _spawnRadius;
        }

        GameObject spawnedItem = Instantiate(_itemPrefab, hit.position + (Vector3.up * 1.5f), Quaternion.identity); // когда попала оно заспавнит предмет

        NetworkServer.Spawn(spawnedItem);
    }
}