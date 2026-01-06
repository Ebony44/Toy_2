using Unity.Collections;
using UnityEngine;

public class FixedStringExample : MonoBehaviour
{
    void Start()
    {
        // Initialize a FixedString32Bytes from a managed string
        FixedString32Bytes myFixedString = new FixedString32Bytes("Hello, Unity Jobs!");

        // Access the string value (can convert back to managed string)
        Debug.Log("String value: " + myFixedString.ToString());

        // Append to the string (may truncate if capacity is exceeded)
        FixedString32Bytes anotherString = new FixedString32Bytes(" More text.");
        myFixedString.Append(anotherString);

        Debug.Log("Appended value: " + myFixedString.ToString());
    }
}
