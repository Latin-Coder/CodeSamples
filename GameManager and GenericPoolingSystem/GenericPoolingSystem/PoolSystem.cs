using System.Collections.Generic;
using UnityEngine;



public class PoolSystem<T>

    where T : MonoBehaviour, IResettable
{
    private Stack<T> poolList = new Stack<T>();

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
            element = Object.Instantiate(prefab, position, rotation, parent);
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
            if (item.activeSelf)
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

