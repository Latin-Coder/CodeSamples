using System.Collections.Generic;
using UnityEngine;



public class PoolSystem<T>

    where T : Component, IResettable
{
    private Stack<T> poolList = new Stack<T>();

    public PoolSystem(int amountToPool, T poolElement)
    {
        poolList = new Stack<T>();

        if (poolElement is MonoBehaviour)
        {
            for (int i = 0; i < amountToPool; i++)
            {
                poolElement = Object.Instantiate(poolElement);
                poolList.Push(poolElement);
            }
        }
        else
        {
            for (int i = 0; i < amountToPool; i++)
            {
                poolElement = default;
                poolList.Push(poolElement);
            }
        }
    }

    private T TakeFromPool()
    {
        T poolElement = null;
        if (poolList.Count > 0)
        {
            poolElement = poolList.Pop();
            poolElement.SetActive(true);
        }
        return poolElement;
    }

    public T PoolAddElement(T prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        T element = TakeFromPool();
        if (element == null)
        {
            if (prefab is MonoBehaviour)
                element = Object.Instantiate(prefab, position, rotation, parent);
            else
                element = default;
        }
        else
        {
            element.transform.position = position;
        }
        return element;
    }
    public void ClearItems(T element)
    {       
            if (element.activeSelf)
            {
                element.SetActive(false);
                poolList.Push(element.GetComponent<T>());
            }       
    }

    public void ClearItems(List<T> elements)
    {
        foreach (T item in elements)
        {
            if (item!= null && item.activeSelf)
            {
                item.SetActive(false);
                poolList.Push(item.GetComponent<T>());
            }
        }
    }

}

public interface IResettable
{
    public void SetActive(bool value);
    public bool activeSelf { get; set; }
}

