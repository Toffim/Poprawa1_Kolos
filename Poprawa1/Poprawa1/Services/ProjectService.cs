namespace Poprawa1.Services;
using System.Data.Common;
using Poprawa1.Models;
using Microsoft.Data.SqlClient;

public class ProjectService : IProjectService
{
    private readonly IConfiguration _configuration;

    public ProjectService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ProjectDTO> GetProjectAsync(int id)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        string sqlQuery = @"
            SELECT PR.ProjectId, PR.StartDate, PR.EndDate, PR.Objective,
            AR.Name AS ArtifactName, AR.OriginDate AS ArtifactOriginDate,
            INST.InstitutionID, INST.Name AS InstitutionName, INST.FoundedYear AS InstitutionFoundedYear,
            STA.Role as Role, STF.FirstName AS StaffFirstName, STF.LastName AS StaffLastName,
            STF.HireDate AS StaffHireDate
            FROM Preservation_Project AS PR
            JOIN Artifact AS AR ON PR.ArtifactId = AR.ArtifactId
            JOIN Institution AS INST ON AR.InstitutionId = INST.InstitutionId
            JOIN Staff_Assignment AS STA ON PR.ProjectId = STA.ProjectId
            JOIN Staff AS STF ON STA.StaffId = STF.StaffId
            WHERE PR.ProjectId = @id
        ";
            
        command.CommandText = sqlQuery;
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();

        ProjectDTO? projectDTO = null;

        while (await reader.ReadAsync())
        {
            if (projectDTO is null)
            {
                int endDate = reader.GetOrdinal("EndDate");
                projectDTO = new ProjectDTO()
                {
                    ProjectId = reader.GetInt32(reader.GetOrdinal("ProjectId")),
                    Objective = reader.GetString(reader.GetOrdinal("Objective")),
                    StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                    EndDate = reader.IsDBNull(endDate) ? null : reader.GetDateTime(reader.GetOrdinal("EndDate"))
                };
            }

            projectDTO.Artifacts.Add(new ArtifactDTO
            {
                Name = reader.GetString(reader.GetOrdinal("ArtifactName")),
                OriginDate = reader.GetDateTime(reader.GetOrdinal("ArtifactOriginDate")),
                Institution = new InstitutionDTO()
                {
                    InsitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                    Name = reader.GetString(reader.GetOrdinal("InstitutionName")),
                    FoundedYear = reader.GetInt32(reader.GetOrdinal("InstitutionFoundedYear")),
                }
            });
            
            projectDTO.StaffAssignments.Add(new StaffAssignmentDTO()
            {
                FirstName = reader.GetString(reader.GetOrdinal("StaffFirstName")),
                LastName = reader.GetString(reader.GetOrdinal("StaffLastName")),
                HireDate = reader.GetDateTime(reader.GetOrdinal("StaffHireDate")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
            });
        }

        if (projectDTO is null)
        {
            throw new ApplicationException($"Could not find project with id {id}");
        }

        return projectDTO;
    }

    public async Task AddArtifactAndProjectAsync(InputDTO input)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            //1 czy artefakt o danym id juz istnieje
            command.Parameters.Clear();
            command.CommandText = "SELECT count(*) FROM Artifact where ArtifactId = @ArtifactId;";
            command.Parameters.AddWithValue("@ArtifactId", input.Artifact.ArtifactId);

            int artifactCount = (int) await command.ExecuteScalarAsync();
            if (artifactCount > 0)
            {
                throw new InvalidOperationException($"An artifact with {input.Artifact.ArtifactId} already exists.");
            }

            //2 czy projekt o id juz istnieje
            command.Parameters.Clear();
            command.CommandText = "SELECT count(*) FROM Preservation_Project where ProjectId = @PRId;";
            command.Parameters.AddWithValue("@PRId", input.Project.ProjectId);

            int projectCount = (int) await command.ExecuteScalarAsync();
            if (projectCount > 0)
            {
                throw new InvalidOperationException($"An project with {input.Project.ProjectId} already exists.");
            }
            
            //3 musimy sprawdzic czy instytut dla artifactu istnieje
            command.Parameters.Clear();
            command.CommandText = "SELECT count(*) FROM Institution where InstitutionId = @InstId;";
            command.Parameters.AddWithValue("@InstId", input.Artifact.InsitutionId);

            int institutionId = (int) await command.ExecuteScalarAsync();
            if (institutionId == 0)
            {
                throw new InvalidOperationException($"An institution with {input.Artifact.InsitutionId} does not exist.");
            }

            //4 wstawianie artifact
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Artifact VALUES (@ArtifactId, @Name, @OriginDate, @InstitutionId)";

            command.Parameters.AddWithValue("@ArtifactId", input.Artifact.ArtifactId);
            command.Parameters.AddWithValue("@Name", input.Artifact.Name);
            command.Parameters.AddWithValue("@OriginDate", input.Artifact.OriginDate);
            command.Parameters.AddWithValue("@InstitutionId", input.Artifact.InsitutionId);

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add artifact: {ex.Message}");
            }
            
            //4 wstawianie project
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Preservation_Project VALUES (@ProjectId, @ArtifactId, @StartDate, @EndDate, @Objective)";

            command.Parameters.AddWithValue("@ProjectId", input.Project.ProjectId);
            command.Parameters.AddWithValue("@ArtifactId", input.Artifact.ArtifactId);
            command.Parameters.AddWithValue("@StartDate", input.Project.StartDate);
            command.Parameters.AddWithValue("@EndDate", input.Project.EndDate);
            command.Parameters.AddWithValue("@Objective", input.Project.Objective);

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add project: {ex.Message}");
            }


            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}