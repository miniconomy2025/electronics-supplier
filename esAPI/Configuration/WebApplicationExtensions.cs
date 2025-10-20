using esAPI.Middleware;

namespace esAPI.Configuration
{
    public static class WebApplicationExtensions
    {
        public static WebApplication ConfigureRequestPipeline(this WebApplication app)
        {
            // Add global exception handling first
            app.UseMiddleware<GlobalExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Use CORS - secure policy for production, permissive for development
            if (app.Environment.IsDevelopment())
            {
                app.UseCors("DevelopmentCors");
            }
            else
            {
                app.UseCors("SecureCorsPolicy");
            }

            // Use Client-Id authentication
            app.UseClientIdentification();

            app.MapControllers();

            return app;
        }
    }
}