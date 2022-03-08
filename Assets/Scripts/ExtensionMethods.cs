using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    public static float[] Deg2Rad(this float[] array)
    {
        float[] radArray = new float[array.Length];
        for(int i = 0; i<array.Length; i++)
        {
            radArray[i] = array[i] * Mathf.Deg2Rad;
        }
        return radArray;
    }

    public static void DrawTransformPosition(this Vector3 position)
    {
        Debug.DrawRay(position, Vector3.right, Color.red);
        Debug.DrawRay(position, Vector3.forward, Color.blue);
        Debug.DrawRay(position, Vector3.up, Color.green);
    }

    public static float EuclideanNorm(this float[] array)
    {
        float cumSum = 0;
        for(int i=0; i < array.Length; i++)
        {
            cumSum += array[i] * array[i];
        }
        return Mathf.Sqrt(cumSum);
    }

    public static float EuclideanDistance(this float[] array, float[] array2)
    {
        float cumSum = 0;
        if(array.Length != array2.Length)
        {
            Debug.Log("Arrays must have the same length.");
            return cumSum;
        }

        for(int i = 0; i < array.Length; i++)
        {
            cumSum += ((array[i] - array2[i]) * (array[i] - array2[i]));
        }
        return Mathf.Sqrt(cumSum);
    }

    public static float[] MultiplyByConstant(this float[] array, float constant)
    {
        float[] arrayMultiplied = new float[array.Length];
        for(int i=0; i < array.Length; i++)
        {
            arrayMultiplied[i] = array[i] * constant;
        }
        return arrayMultiplied;
    }
    
    public static void PrintArray(this float[] array)
    {
        string stringToPrint = "[ ";
        foreach(float item in array) stringToPrint += (item.ToString() + " , ");
        stringToPrint += " ]";
        Debug.Log(stringToPrint);
    }

    public static string ArrayToString(this float[] array)
    {
        string newString = " ";
        foreach(float item in array) newString += (item.ToString() + " ");
        //Debug.Log(newString);
        return newString;
    }

    public static float[] Rad2Deg(this float[] array)
    {
        float[] degArray = new float[array.Length];
        for(int i = 0; i<array.Length; i++)
        {
            degArray[i] = array[i] * Mathf.Rad2Deg;
        }
        return degArray;
    }

}
