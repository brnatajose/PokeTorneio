using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PokeTorneio.Data;
using PokeTorneio.Services;

var builder = WebApplication.CreateBuilder(args);

// Configura a string de conex�o com SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Log para depura��o
Console.WriteLine($"Connection String: {connectionString}");

// Adiciona o contexto do banco de dados usando SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Configura��o do Identity com o ApplicationDbContext
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true; // Requer confirma��o de conta
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Registra os servi�os necess�rios
builder.Services.AddScoped<ITorneioService, TorneioService>();

// Adiciona suporte a controladores e views
builder.Services.AddControllersWithViews();

// Cria a aplica��o
var app = builder.Build();

// Configura��o do middleware
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HSTS (HTTP Strict Transport Security)
}

app.UseHttpsRedirection(); // Redireciona HTTP para HTTPS
app.UseStaticFiles(); // Permite servir arquivos est�ticos
app.UseRouting(); // Habilita o roteamento
app.UseAuthentication(); // Habilita a autentica��o
app.UseAuthorization(); // Habilita a autoriza��o

// Inicializa o banco de dados
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated(); // Cria o banco de dados se n�o existir
}

// Configura��o das rotas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // Mapear p�ginas Razor

// Inicia a aplica��o
app.Run();
