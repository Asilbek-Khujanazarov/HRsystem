public class EmployeeRegisterDto
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public string? Role { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public required string Position { get; set; }
    public required string City { get; set; }
    public required string JobTitle { get; set; }
    public int DepartmentId { get; set; }
}

public class EmployeeLoginDto
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
}

public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public required string UserName { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public required string Position { get; set; }
    public required string City { get; set; }
    public required string JobTitle { get; set; }
    public string? ProfileImagePath { get; set; }
    public required string Role { get; set; }
}

public class EmployeeNameDto
{
    public int EmployeeId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}
