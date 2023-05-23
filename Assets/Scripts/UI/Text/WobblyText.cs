using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WobblyText : MonoBehaviour
{
    [SerializeField] private TMP_Text _textMesh;
    [SerializeField] private float _speed = 1.5f;
    [SerializeField] private float _amplitude = 15.75f;

    private void OnValidate() 
    {
        TryGetComponent<TMP_Text>(out _textMesh);
    }

    private void Update()
    {
        ApplyEffect();
    }

    private void ApplyEffect()
    {
        _textMesh.ForceMeshUpdate();
        var textInfo = _textMesh.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) { continue; }

            var verts = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

            for (int v = 0; v < 4; v++)
            {
                var orig = verts[charInfo.vertexIndex + v];
                verts[charInfo.vertexIndex + v] = orig + Vector3.up * Mathf.Sin(Time.time * _speed + verts[charInfo.vertexIndex].x * 0.01f) * _amplitude;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;

            _textMesh.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}
