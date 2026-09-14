using System.ComponentModel.DataAnnotations;

//Emeris School of Computer Science, 2025
namespace backendAPI.Models
{
    public class DeploymentNode
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public int? ParentId { get; set; }
        public DeploymentNode? Parent { get; set; }

        public List<DeploymentNode> Children { get; set; } = new();
    }
}