using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador de greybox para el hospital (SCRUM-7).
/// Cada sala se define como un rectángulo (centro + tamaño en X/Z) con "puertas"
/// (huecos en las paredes). El script genera piso, techo y muros con cubos.
///
/// USO:
/// 1) Crea un GameObject vacío llamado "HospitalGreybox".
/// 2) Añade este componente.
/// 3) Click derecho en el componente -> "Cargar layout sugerido (plano 1er piso)".
/// 4) Click derecho -> "Generar Greybox".
/// 5) Ajusta posiciones/tamaños/puertas en el Inspector o arrastrando en la Scene view
///    y vuelve a generar (borra lo anterior automáticamente).
/// </summary>
public class HospitalGreyboxBuilder : MonoBehaviour
{
    public enum WallSide { North, South, East, West }

    [System.Serializable]
    public class DoorGap
    {
        public WallSide wall;
        [Range(0f, 1f)] public float position = 0.5f; // posición normalizada a lo largo del muro
        public float width = 1.2f;                    // ancho de la puerta en metros
    }

    [System.Serializable]
    public class RoomData
    {
        public string roomName = "Sala";
        public Vector2 center = Vector2.zero;        // x, z (en metros)
        public Vector2 size = new Vector2(5f, 5f);    // ancho (x), profundo (z)
        public List<DoorGap> doors = new List<DoorGap>();

        // Para salas "isla" que quedan dentro del footprint de otra sala (p.ej. un
        // mostrador de recepción dentro del Lobby): evita duplicar piso/techo
        // exactamente encima del de la sala contenedora (z-fighting).
        public bool skipFloorAndCeiling = false;

        // Lados sin muro en absoluto (a diferencia de una puerta, aquí no se
        // construye ningún segmento). Útil para pasillos abiertos que se funden
        // con la sala vecina en vez de tener una abertura puntual.
        public List<WallSide> openSides = new List<WallSide>();
    }

    [Header("Salas")]
    public List<RoomData> rooms = new List<RoomData>();

    [Header("Dimensiones")]
    public float wallHeight = 4.5f;
    public float wallThickness = 0.3f;
    public float floorThickness = 0.2f;

    [Header("Materiales (opcional)")]
    public Material wallMaterial;
    public Material floorMaterial;
    public Material ceilingMaterial;

    // ---------------------------------------------------------------
    // GENERACIÓN
    // ---------------------------------------------------------------

    [ContextMenu("Generar Greybox")]
    public void Build()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            DestroyImmediate(child);
#else
            Destroy(child);
#endif
        }

        foreach (var room in rooms)
            BuildRoom(room);
    }

    void BuildRoom(RoomData room)
    {
        GameObject roomGO = new GameObject(room.roomName);
        roomGO.transform.parent = transform;
        roomGO.transform.localPosition = Vector3.zero;

        float xMin = room.center.x - room.size.x / 2f;
        float xMax = room.center.x + room.size.x / 2f;
        float zMin = room.center.y - room.size.y / 2f;
        float zMax = room.center.y + room.size.y / 2f;

        if (!room.skipFloorAndCeiling)
        {
            CreateCube(roomGO.transform, room.roomName + "_Piso",
                new Vector3(room.center.x, -floorThickness / 2f, room.center.y),
                new Vector3(room.size.x, floorThickness, room.size.y), floorMaterial);

            CreateCube(roomGO.transform, room.roomName + "_Techo",
                new Vector3(room.center.x, wallHeight + floorThickness / 2f, room.center.y),
                new Vector3(room.size.x, floorThickness, room.size.y), ceilingMaterial);
        }

        if (!room.openSides.Contains(WallSide.North))
            BuildWall(roomGO.transform, room, WallSide.North, xMin, xMax, zMax);
        if (!room.openSides.Contains(WallSide.South))
            BuildWall(roomGO.transform, room, WallSide.South, xMin, xMax, zMin);
        if (!room.openSides.Contains(WallSide.East))
            BuildWall(roomGO.transform, room, WallSide.East, zMin, zMax, xMax);
        if (!room.openSides.Contains(WallSide.West))
            BuildWall(roomGO.transform, room, WallSide.West, zMin, zMax, xMin);
    }

    void BuildWall(Transform parent, RoomData room, WallSide side, float from, float to, float fixedCoord)
    {
        bool horizontal = (side == WallSide.North || side == WallSide.South);
        float length = to - from;

        List<DoorGap> doorsOnThisWall = room.doors.FindAll(d => d.wall == side);

        List<(float start, float end)> gaps = new List<(float, float)>();
        foreach (var door in doorsOnThisWall)
        {
            float center = from + door.position * length;
            gaps.Add((center - door.width / 2f, center + door.width / 2f));
        }
        gaps.Sort((a, b) => a.start.CompareTo(b.start));

        float cursor = from;
        foreach (var gap in gaps)
        {
            float segStart = cursor;
            float segEnd = Mathf.Max(cursor, gap.start);
            if (segEnd - segStart > 0.05f)
                CreateWallSegment(parent, room.roomName, side, horizontal, segStart, segEnd, fixedCoord);
            cursor = Mathf.Max(cursor, gap.end);
        }
        if (to - cursor > 0.05f)
            CreateWallSegment(parent, room.roomName, side, horizontal, cursor, to, fixedCoord);
    }

    void CreateWallSegment(Transform parent, string roomName, WallSide side, bool horizontal, float from, float to, float fixedCoord)
    {
        float length = to - from;
        float mid = (from + to) / 2f;

        Vector3 position;
        Vector3 size;

        if (horizontal) // North/South: el muro corre en X, Z fijo
        {
            position = new Vector3(mid, wallHeight / 2f, fixedCoord);
            size = new Vector3(length, wallHeight, wallThickness);
        }
        else // East/West: el muro corre en Z, X fijo
        {
            position = new Vector3(fixedCoord, wallHeight / 2f, mid);
            size = new Vector3(wallThickness, wallHeight, length);
        }

        CreateCube(parent, $"{roomName}_Muro_{side}_{from:F1}", position, size, wallMaterial);
    }

    void CreateCube(Transform parent, string name, Vector3 position, Vector3 size, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.parent = parent;
        cube.transform.position = position;
        cube.transform.localScale = size;
        if (mat != null)
            cube.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ---------------------------------------------------------------
    // LAYOUT SUGERIDO (basado en el boceto del 1er piso)
    // Escala aproximada: hospital de ~50m x 27m. AJUSTA LIBREMENTE.
    //
    // Todo el piso vive bajo el mismo techo, repartido en dos bloques
    // separados por x = 18, unidos por un pasillo pasante (PasilloCentral)
    // que atraviesa esa frontera y conecta Oficinas con el Lobby:
    //
    //   Bloque oeste (x: 0-18)      Bloque este (x: 18-50)
    //   ----------------------      -----------------------------
    //   Oficinas      (z: 12.5-27)  Lobby            (z: 12.5-27, ancho completo)
    //     -> puerta este hacia el     -> puerta oeste hacia el pasillo (misma
    //        pasillo (mismo hueco)       franja de Z que la de Oficinas)
    //   Consultorios  (z: 0-12.5)   CuartoSeguridad  (z: 0-12.5, x: 18-31.5)
    //                               SalaEste         (z: 0-12.5, x: 31.5-50, sin definir aún)
    //
    // PasilloCentral (x: 18-29.7, z: 14.8-22.1): abierto (sin muro) al este y al
    // oeste, se funde con el Lobby y con la abertura de Oficinas. Tiene además
    // dos ramales con puerta, norte y sur, hacia el resto del Lobby (el del sur
    // queda cerca de la puerta de CuartoSeguridad).
    // ---------------------------------------------------------------

    private void Reset()
    {
        rooms = GetSuggestedLayout();
    }

    [ContextMenu("Cargar layout sugerido (plano 1er piso)")]
    public void LoadSuggestedLayout()
    {
        rooms = GetSuggestedLayout();
    }

    private List<RoomData> GetSuggestedLayout()
    {
        var list = new List<RoomData>();

        // Oficinas (arriba izquierda)
        var oficinas = new RoomData { roomName = "Oficinas", center = new Vector2(9f, 19.75f), size = new Vector2(18f, 14.5f) };
        oficinas.doors.Add(new DoorGap { wall = WallSide.South, position = 0.58f, width = 1.5f }); // hacia consultorios
        oficinas.doors.Add(new DoorGap { wall = WallSide.East, position = 0.41f, width = 7.3f });  // hacia el pasillo central
        list.Add(oficinas);

        // Consultorios (abajo izquierda, bloque grande - internamente sin subdividir por ahora)
        var consultorios = new RoomData { roomName = "Consultorios", center = new Vector2(9f, 6.25f), size = new Vector2(18f, 12.5f) };
        consultorios.doors.Add(new DoorGap { wall = WallSide.North, position = 0.58f, width = 1.5f }); // hacia oficinas (misma puerta, en espejo)
        list.Add(consultorios);

        // Lobby (mitad derecha, fila superior, ancho completo del bloque este)
        var lobby = new RoomData { roomName = "Lobby", center = new Vector2(34f, 19.75f), size = new Vector2(32f, 14.5f) };
        lobby.doors.Add(new DoorGap { wall = WallSide.South, position = 0.073f, width = 1.5f }); // hacia cuarto de seguridad
        lobby.doors.Add(new DoorGap { wall = WallSide.West, position = 0.41f, width = 7.3f });    // hacia el pasillo central (misma franja que la puerta de Oficinas)
        list.Add(lobby);

        // Pasillo central: atraviesa la frontera entre el bloque oeste (Oficinas)
        // y el Lobby, fundiéndose con ambos (sin muro este/oeste). Tiene dos
        // ramales con puerta (norte y sur) hacia el resto del Lobby. No genera
        // piso/techo propio porque su footprint ya está cubierto por el del Lobby.
        var pasilloCentral = new RoomData { roomName = "PasilloCentral", center = new Vector2(23.85f, 18.45f), size = new Vector2(11.7f, 7.3f), skipFloorAndCeiling = true };
        pasilloCentral.openSides.Add(WallSide.East);
        pasilloCentral.openSides.Add(WallSide.West);
        pasilloCentral.doors.Add(new DoorGap { wall = WallSide.North, position = 0.5f, width = 3.5f });
        pasilloCentral.doors.Add(new DoorGap { wall = WallSide.South, position = 0.5f, width = 3.5f });
        list.Add(pasilloCentral);

        // Cuarto de seguridad (centro-abajo)
        var seguridad = new RoomData { roomName = "CuartoSeguridad", center = new Vector2(24.75f, 6.25f), size = new Vector2(13.5f, 12.5f) };
        seguridad.doors.Add(new DoorGap { wall = WallSide.North, position = 0.13f, width = 1.5f }); // hacia lobby
        list.Add(seguridad);

        // Sala sin nombre, abajo derecha - pendiente definir su función (sin puertas por ahora)
        var salaEste = new RoomData { roomName = "SalaEste", center = new Vector2(40.75f, 6.25f), size = new Vector2(18.5f, 12.5f) };
        list.Add(salaEste);

        return list;
    }
}
