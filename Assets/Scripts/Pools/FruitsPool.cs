using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitsPool : MonoBehaviour
{
    [SerializeField] private Transform[] _fruits;
    [SerializeField] private Dictionary<string, PoolBase<Transform>> _fruitsDictionary = new Dictionary<string, PoolBase<Transform>>();
    private int _randNum;
    public Dictionary<string, PoolBase<Transform>> FruitsDictionary { get => _fruitsDictionary; set => _fruitsDictionary = value; }
    public Transform[] Fruits { get => _fruits; set => _fruits = value; }


    private void Awake()
    {
        for (int i = 0; i < Fruits.Length; i++)
            _fruitsDictionary.Add($"{i}", new PoolBase<Transform>(Fruits[i], 0, transform));
    }
    public Transform UsePool(Transform posTransform)
    {
        _randNum = Random.Range(0, Fruits.Length);
        Transform spawnedFruit = FruitsDictionary[$"{_randNum}"].GetObjectFromPool();
        spawnedFruit.transform.position = posTransform.position;
        spawnedFruit.name += $"{_randNum}";

        return spawnedFruit;
    }

    public void TurnOff(Transform fruit)
    {
        FruitsDictionary[$"{_randNum}"].ObjectOff(fruit);
        Manager.Instance.ParticlesPool.UsePool(fruit, _randNum);
    }

    public void DisableAllFruits()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

    }
}
