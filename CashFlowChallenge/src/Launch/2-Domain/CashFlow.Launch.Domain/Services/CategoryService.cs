using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;

namespace CashFlow.Launch.Domain.Services;

public class CategoryService : ICategoryService
{
    private static readonly (string Name, EntryType Type)[] DefaultCategories =
    [
        ("Salário", EntryType.Credit),
        ("13º salário", EntryType.Credit),
        ("Férias", EntryType.Credit),
        ("Horas extras", EntryType.Credit),
        ("Freelance / Extra", EntryType.Credit),
        ("Aluguel recebido", EntryType.Credit),
        ("Benefícios", EntryType.Credit),
        ("Rendimentos / Investimentos", EntryType.Credit),
        ("Reembolso", EntryType.Credit),
        ("Venda", EntryType.Credit),
        ("Outras receitas", EntryType.Credit),

        ("Moradia", EntryType.Debit),
        ("Financiamento / Aluguel", EntryType.Debit),
        ("Condomínio", EntryType.Debit),
        ("Água", EntryType.Debit),
        ("Energia", EntryType.Debit),
        ("Internet / Telefone", EntryType.Debit),
        ("Supermercado", EntryType.Debit),
        ("Alimentação / Restaurante", EntryType.Debit),
        ("Transporte", EntryType.Debit),
        ("Combustível", EntryType.Debit),
        ("Veículo / Manutenção", EntryType.Debit),
        ("Saúde", EntryType.Debit),
        ("Farmácia", EntryType.Debit),
        ("Educação", EntryType.Debit),
        ("Escola", EntryType.Debit),
        ("Material escolar", EntryType.Debit),
        ("Filhos", EntryType.Debit),
        ("Lazer", EntryType.Debit),
        ("Viagem", EntryType.Debit),
        ("Assinaturas / Streaming", EntryType.Debit),
        ("Roupas", EntryType.Debit),
        ("Cuidados pessoais", EntryType.Debit),
        ("Casa / Manutenção", EntryType.Debit),
        ("Pets", EntryType.Debit),
        ("Seguros", EntryType.Debit),
        ("Impostos / Taxas", EntryType.Debit),
        ("Empréstimos / Dívidas", EntryType.Debit),
        ("Presentes", EntryType.Debit),
        ("Doações", EntryType.Debit),
        ("Reserva / Investimentos", EntryType.Debit),
        ("Outras despesas", EntryType.Debit)
    ];

    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Category> CreateAsync(string name, EntryType type)
    {
        var category = new Category
        {
            Name = name.Trim(),
            Type = type
        };

        await _repository.AddAsync(category);
        await _repository.SaveChangesAsync();
        return category;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        var categories = await _repository.GetAllAsync();
        var existing = categories
            .Select(category => (Normalize(category.Name), category.Type))
            .ToHashSet();

        var missing = DefaultCategories
            .Where(category => !existing.Contains((Normalize(category.Name), category.Type)))
            .Select(category => new Category
            {
                Name = category.Name,
                Type = category.Type
            })
            .ToList();

        if (missing.Count == 0)
            return Order(categories);

        foreach (var category in missing)
            await _repository.AddAsync(category);

        await _repository.SaveChangesAsync();
        categories.AddRange(missing);

        return Order(categories);
    }

    public Task<Category?> GetByIdAsync(Guid id) => _repository.GetByIdAsync(id);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static List<Category> Order(IEnumerable<Category> categories) => categories
        .OrderBy(category => category.Type)
        .ThenBy(category => category.Name, StringComparer.Create(new System.Globalization.CultureInfo("pt-BR"), true))
        .ToList();
}
