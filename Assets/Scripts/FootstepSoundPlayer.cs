using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class FootstepSoundPlayer : MonoBehaviour
{
    [SerializeField]
    private LayerMask FloorLayer;
    [SerializeField]
    private TextureSound[] TextureSounds;
    [SerializeField]
    private bool BlendTerrainSounds;

    private CharacterController Controller;
    private AudioSource AudioSource;

    private void Awake()
    {
        Controller = GetComponent<CharacterController>();
        AudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        StartCoroutine(CheckGround());
    }

    private IEnumerator CheckGround()
    {
        while (true)
        {
            if (Controller.isGrounded && Controller.velocity != Vector3.zero &&
                Physics.Raycast(transform.position - new Vector3(0, 0.5f * Controller.height + 0.5f * Controller.radius, 0),
                    Vector3.down,
                    out RaycastHit hit,
                    1f,
                    FloorLayer)
                )
            {
                if (hit.collider.TryGetComponent<Terrain>(out Terrain terrain))
                {
                    yield return StartCoroutine(PlayFootstepSoundFromTerrain(terrain, hit.point));
                }
                else if (hit.collider.TryGetComponent<Renderer>(out Renderer renderer))
                {
                    yield return StartCoroutine(PlayFootstepSoundFromRenderer(renderer));
                }
            }

            yield return null;
        }
    }

    private IEnumerator PlayFootstepSoundFromTerrain(Terrain Terrain, Vector3 HitPoint)
    {
        Vector3 terrainPosition = HitPoint - Terrain.transform.position;
        Vector3 splatMapPosition = new Vector3(
            terrainPosition.x / Terrain.terrainData.size.x,
            0,
            terrainPosition.z / Terrain.terrainData.size.z
        );

        int x = Mathf.FloorToInt(splatMapPosition.x * Terrain.terrainData.alphamapWidth);
        int z = Mathf.FloorToInt(splatMapPosition.z * Terrain.terrainData.alphamapHeight);

        float[,,] alphaMap = Terrain.terrainData.GetAlphamaps(x, z, 1, 1);

        if (!BlendTerrainSounds)
        {
            int primaryIndex = 0;
            for (int i = 1; i < alphaMap.Length; i++)
            {
                if (alphaMap[0, 0, i] > alphaMap[0, 0, primaryIndex])
                {
                    primaryIndex = i;
                }
            }

            foreach(TextureSound textureSound in TextureSounds)
            {
                if (textureSound.Albedo == Terrain.terrainData.terrainLayers[primaryIndex].diffuseTexture)
                {
                    AudioClip clip = GetClipFromTextureSound(textureSound);
                    AudioSource.PlayOneShot(clip);
                    yield return new WaitForSeconds(clip.length);
                    break;
                }
            }
        }
        else
        {
            List<AudioClip> clips = new List<AudioClip>();
            int clipIndex = 0;
            for (int i = 0; i < alphaMap.Length; i++)
            {
                if (alphaMap[0, 0, i] > 0)
                {
                    foreach (TextureSound textureSound in TextureSounds)
                    {
                        if (textureSound.Albedo == Terrain.terrainData.terrainLayers[i].diffuseTexture)
                        {
                            AudioClip clip = GetClipFromTextureSound(textureSound);
                            AudioSource.PlayOneShot(clip, alphaMap[0, 0, i]);
                            clips.Add(clip);
                            clipIndex++;
                            break;
                        }
                    }
                }
            }

            float longestClip = clips.Max(clip => clip.length);

            yield return new WaitForSeconds(longestClip);
        }
    }

    private IEnumerator PlayFootstepSoundFromRenderer(Renderer Renderer)
    {
        foreach (TextureSound textureSound in TextureSounds)
        {
            if (textureSound.Albedo == Renderer.material.GetTexture("_MainTex"))
            {
                AudioClip clip = GetClipFromTextureSound(textureSound);

                AudioSource.PlayOneShot(clip);
                yield return new WaitForSeconds(clip.length);
                break;
            }
        }
    }

    private AudioClip GetClipFromTextureSound(TextureSound TextureSound)
    {
        int clipIndex = Random.Range(0, TextureSound.Clips.Length);
        return TextureSound.Clips[clipIndex];
    }

    [System.Serializable]
    private class TextureSound
    {
        public Texture Albedo;
        public AudioClip[] Clips;
    }
}
