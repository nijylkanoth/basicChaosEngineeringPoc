using ChaosEngineeringApi.Models;

namespace ChaosEngineeringApi.Services;

public class TodoClientService(HttpClient httpClient)
{
    public async Task<IEnumerable<TodoModel>> GetTodosAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<IEnumerable<TodoModel>>("/todos",cancellationToken) ?? [];
    }
}

