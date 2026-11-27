using UnityEngine;

namespace Assets.Scripts
{
    public class DatabaseConnection : MonoBehaviour
    {

        // Define the connection into a var, use for creating queries.
        // DO NOT COMMIT WITH THE CONNECTIONSTRING FILLED!
        const string DbConnectionStringBuilder = "<here put the connection strings, ONLY FOR LOCAL / BUILD>";

        // Define the table structure.

        // Function to select from the database (change void to object).
        public void GetData()
        { 
            return;
        }

        // Function to add into the database (bool based on success).
        public bool InsertData()
        {
            return false;
        }
    }
}