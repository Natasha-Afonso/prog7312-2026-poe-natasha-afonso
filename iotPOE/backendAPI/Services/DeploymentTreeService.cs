using backendAPI.Models;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    public class DeploymentTreeService
    {
    //Recursion
        public bool ValidateDeploymentPath(DeploymentNode currentNode, string targetName, string currentPath, out string fullPath)
        {
            if (string.IsNullOrEmpty(currentPath))
            {
                fullPath = currentNode.Name;
            }
            else
            {
                fullPath = currentPath + " : " + currentNode.Name;
            }

            if (currentNode.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (DeploymentNode childNode in currentNode.Children)
            {
                bool foundInChild = ValidateDeploymentPath(childNode, targetName, fullPath, out string childPath);
                if (foundInChild == true)
                {
                    fullPath = childPath;
                    return true;
                }
            }

            return false;
        }
    }
}