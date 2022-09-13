using UnityEngine;
using UnityEditor;

namespace Week02
{

    // In this example we will see how the input that we provide in the Unity transform can be used
    // to create a transformation matrix
    [ExecuteInEditMode] // this makes the script run without playing the scene
    public class TransformMatrix : MonoBehaviour
    {
        // variables to store the transformation parameters
        //public Vector3 Position = Vector3.zero;
        //public Vector3 Rotation = Vector3.zero;
        //public Vector3 Scale = Vector3.one;

        Matrix4x4 transformation = Matrix4x4.identity;

        // Called when script is loaded or a value is changed in the inspector 
        private void Update()
        {
            Vector3 position = transform.position;
            Vector3 rotation = transform.rotation.eulerAngles;
            Vector3 scale = transform.lossyScale;

            // Unity already include a function to create a transformation matrix
            // using Position, Rotation and Scale:
            // transformation = Matrix4x4.TRS(Position, Quaternion.Euler(Rotation), Scale);
            // but we want to create our own!

            transformation = OurTRS(position, rotation, scale);


        }

        Matrix4x4 OurTRS(Vector3 position, Vector3 rotation, Vector3 scale) {
            // create three rotation matrices, one per rotation axis/euler angle

            // create the translation matrix
            Matrix4x4 T = Matrix4x4.identity;
            T.m03 = position.x;
            T.m13 = position.y;
            T.m23 = position.z;

            // create the rotation matrix
            Matrix4x4 S = Matrix4x4.identity;
            S.m00 = scale.x;
            S.m11 = scale.y;
            S.m22 = scale.z;

            // rotationX is given as an example
            Matrix4x4 RX = Matrix4x4.identity;
            float angleRad = Mathf.Deg2Rad * rotation.x;
            RX.m11 = Mathf.Cos(angleRad);
            RX.m21 = Mathf.Sin(angleRad);
            RX.m12 = -Mathf.Sin(angleRad);
            RX.m22 = Mathf.Cos(angleRad);

            Matrix4x4 RY = Matrix4x4.identity;

            Matrix4x4 RZ = Matrix4x4.identity;

            // remember that, when using euler angles, rotation orther matters!
            // by default, Unity implements the order Rotation Z -> then X -> then Y
            Matrix4x4 R = Matrix4x4.identity; // R = multiply RX, RY and RZ in the correct order

            // we concatenate the transformations into a single matrix
            // notices that, since we use column vectors, 
            // transformations are applied from right to left
            // that means first Scale -> then Rotation -> and Translation last
            return Matrix4x4.identity; // return multiply translation, scale and rotation in the correct order;
        }


        // draw debug objects
        void OnDrawGizmos()
        {
            // we apply the transformation, now all our Gizmos.Draw... calls will use this matrix
            Gizmos.matrix = transformation;

            // Draw a cube to show the effect of the transformation matrix
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(0, 0, 0), Vector3.one * 1.005f);
            Gizmos.color = Color.grey * 0.25f;
            Gizmos.DrawCube(new Vector3(0, 0, 0), Vector3.one * 1.005f);


            // we pop the transformation matrix and replace it with the identity matrix
            // that is the matrix that will not affect other matrices or points/vectors
            // we don't need it in this example, but it is a good practice to avoid hard to detect
            // bugs if we modify the scene later
            Gizmos.matrix = Matrix4x4.identity;

        }
    }
}