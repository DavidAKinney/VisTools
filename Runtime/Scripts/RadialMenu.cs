using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IVLab.VisTools
{
    public class RadialMenu
    {
        public RadialMenu(string[] sectorLabels, Color baseColor, Color textColor, Color highlightColor,
                          float scale = 1, string objectName = "Radial Menu", float startingAngle = Mathf.PI / 2)
        {
            // Adjust this to determine how many vertices the menu's background will have. 
            int circleVertexCount = 40;

            this.baseColor = baseColor;
            this.highlightColor = highlightColor;
            menuScale = scale;
            angleOffset = startingAngle;
            labels = sectorLabels;
            currentSectorIndex = 0;
            sectors = new List<GameObject>();

            // Get the number of sectors that are needed based on the number of labels being passed in.
            sectorCount = Mathf.Max(sectorLabels.Length, 2);

            // Get the radial angle that each slice of the menu will take up.
            sliceAngle = (2 * Mathf.PI) / sectorCount;

            // Check to make sure the angle offset is within range of the unit circle.
            if (startingAngle < 0 || startingAngle > 2 * Mathf.PI)
            {
                angleOffset = 0;
            }

            // Find the Unlit/Color shader and save it for later use.
            Shader unlitWithColor = Shader.Find("Unlit/Color");

            // Instantiate the base game object's components for the menu.
            menuObject = new GameObject(objectName);

            // Create a list for the menu's sector vertices and calculate a sampling increment.
            List<List<Vector3>> sectorVertexLists = new List<List<Vector3>>();
            float samplingIncrement = (2 * Mathf.PI) / circleVertexCount;

            // Iterate around the menu's sectors and sample points along it that are scaled by the menu's size.
            for (int i = 0; i < sectorCount; i++)
            {
                sectorVertexLists.Add(new List<Vector3>());
                float sectorStart = angleOffset + (i * sliceAngle);
                for (float angle = sectorStart; angle <= sectorStart + sliceAngle; angle += samplingIncrement)
                {
                    sectorVertexLists[i].Add(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * menuScale);
                }
            }

            // Create a flattened copy of the sector vertices.
            List<Vector3> circleVertices = new List<Vector3>();
            for (int i = 0; i < sectorVertexLists.Count; i++)
            {
                circleVertices.AddRange(sectorVertexLists[i]);
            }

            // Create a circular border around the menu area with a line renderer.
            GameObject menuBorder = new GameObject("Menu Border");
            menuBorder.transform.SetParent(menuObject.transform);
            menuBorder.layer = 5;
            LineRenderer borderLine = menuBorder.AddComponent<LineRenderer>();
            borderLine.useWorldSpace = false;
            borderLine.loop = true;
            borderLine.material.shader = unlitWithColor;
            borderLine.material.color = highlightColor;
            borderLine.startWidth = 0.001f;
            borderLine.endWidth = 0.001f;
            borderLine.positionCount = circleVertices.Count;
            borderLine.SetPositions(circleVertices.ToArray());

            // Create arc meshes for each sector.
            for (int i = 0; i < sectorVertexLists.Count; i++)
            {
                // Add the first vertex of the next sector so that the meshes are flush.
                int nextSectorIndex = (i + 1) % sectorVertexLists.Count;
                Vector3 lastVertex = sectorVertexLists[nextSectorIndex][0];
                sectorVertexLists[i].Add(lastVertex);

                // Add the origin vertex to each sector.
                sectorVertexLists[i].Add(Vector3.zero);

                // Create triangles using the vertices of each sector.
                List<int> triangleIndices = new List<int>();
                for (int j = 0; j < sectorVertexLists[i].Count - 2; j++)
                {
                    triangleIndices.Add(j);
                    triangleIndices.Add(sectorVertexLists[i].Count - 1);
                    triangleIndices.Add(j + 1);
                }     
                
                // Instantiate the sector mesh and material.
                GameObject sectorObject = new GameObject("Sector " + (i + 1));
                sectorObject.layer = 5;
                sectorObject.transform.SetParent(menuObject.transform);
                sectorObject.AddComponent<MeshFilter>();
                sectorObject.AddComponent<MeshRenderer>();
                Mesh sectorMesh = sectorObject.GetComponent<MeshFilter>().mesh;
                sectorMesh.vertices = sectorVertexLists[i].ToArray();
                sectorMesh.triangles = triangleIndices.ToArray();
                sectorObject.GetComponent<MeshRenderer>().material = new Material(unlitWithColor);
                sectorObject.GetComponent<MeshRenderer>().material.color = baseColor;
                sectors.Add(sectorObject);
            }

            float sliceIncrement = angleOffset;

            // Create the menu dividers and label the sectors.
            for (int i = 0; i < sectorCount; i++)
            {
                Vector3[] endpoints = { Vector3.zero, new Vector3(Mathf.Cos(sliceIncrement), Mathf.Sin(sliceIncrement), -0.01f) * menuScale };
                GameObject menuDivider = new GameObject("Slice Divider " + i);
                menuDivider.layer = 5;
                menuDivider.transform.SetParent(menuObject.transform);
                LineRenderer dividerLine = menuDivider.AddComponent<LineRenderer>();
                dividerLine.useWorldSpace = false;
                dividerLine.alignment = LineAlignment.TransformZ;
                dividerLine.material.shader = unlitWithColor;
                dividerLine.material.color = new Color(0, 1, 1);
                dividerLine.loop = false;
                dividerLine.startWidth = 0.001f;
                dividerLine.endWidth = 0.001f;
                dividerLine.SetPositions(endpoints);

                // Create a position vector for the slice's label and create the label.
                Vector3 labelPosition = new Vector3(Mathf.Cos(sliceIncrement + sliceAngle/2), Mathf.Sin(sliceIncrement + sliceAngle/2), -0.01f) * 0.05f;
                GameObject label = new GameObject("Slice Label " + i);
                label.layer = 5;
                label.transform.localPosition = labelPosition;
                label.transform.SetParent(menuObject.transform);
                TextMesh text = label.AddComponent<TextMesh>();
                text.text = sectorLabels[i];
                text.characterSize = 0.008f;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.color = textColor;

                // Increment the angle for the next slice.
                sliceIncrement = (sliceIncrement + sliceAngle) % (Mathf.PI * 2);
            }

            // Finally, highlight the first sector.
            SetSectorState(currentSectorIndex);
        }

        // This function returns the sector index of a menu selection with the joystick.
        public int GetSectorSelection(Vector2 joystickAxis)
        {
            // Find the unit circle angle of the joystick axis.
            float joystickAngle = Mathf.Deg2Rad * Vector2.Angle(new Vector2(1,0), joystickAxis);
            if (joystickAxis.y < 0)
            {
                joystickAngle = (2 * Mathf.PI) - joystickAngle;
            }

            // Loop through the sectors and find which sector the joystick is angled towards.
            for (int i = 0; i < sectorCount; i++)
            {
                float sectorStart = angleOffset + (i * sliceAngle);
                float sectorEnd = (sectorStart + sliceAngle) % (Mathf.PI * 2);

                // Account for wrapping around the unit circle when comparing slice areas.
                if (sectorStart < sectorEnd)
                {
                    if (joystickAngle >= sectorStart && joystickAngle < sectorEnd)
                    {
                        Debug.Log("Switching to state: " + labels[i]);
                        SetSectorState(i);
                        return i;
                    }
                }
                else if (sectorStart > sectorEnd)
                {
                    if (joystickAngle >= sectorStart || (joystickAngle >= 0 && joystickAngle < sectorEnd))
                    {
                        Debug.Log("Switching to state: " + labels[i]);
                        SetSectorState(i);
                        return i;
                    } 
                }
            }

            Debug.Log("No valid menu state found.");
            return -1;
        }

        // This function updates and colors the menu based on a new sector index.
        public void SetSectorState(int newSectorIndex)
        {
            // Return early if the index is out of bounds.
            if (newSectorIndex < 0 || newSectorIndex >= sectors.Count)
            {
                return;
            }

            // Reset the previous sector before updating the next.
            sectors[currentSectorIndex].GetComponent<MeshRenderer>().material.color = baseColor;
            currentSectorIndex = newSectorIndex;
            sectors[currentSectorIndex].GetComponent<MeshRenderer>().material.color = highlightColor;
        }

        // This function sets the parent of the menu's transform.
        public void setMenuParent(Transform parentTransform)
        {
            menuObject.transform.SetParent(parentTransform);
        }

        // This function sets the local position of the menu.
        public void setLocalPosition(Vector3 newPosition)
        {
            menuObject.transform.localPosition = newPosition;
        }

        private int sectorCount;
        private int currentSectorIndex;
        private float menuScale;
        private float angleOffset;
        private float sliceAngle;
        private GameObject menuObject;
        private List<GameObject> sectors;
        private Color baseColor;
        private Color highlightColor;
        private string[] labels;
    }
}
