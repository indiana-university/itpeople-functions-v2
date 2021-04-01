namespace Models

{
    public class ToolPermission : Entity
    {
        /// The netid of the grantee
        public string Netid { get; }
        /// The name of the tool to which permissions have been granted 
        public string  ToolName { get;}
        /// For department-scoped tools, the name of the department
        public string  DepartmentName { get; }
    }
}