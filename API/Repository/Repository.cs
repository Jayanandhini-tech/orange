using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;


namespace CMS.API.Repository;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDBContext _db;
    private readonly int vendorId;
    private readonly string appType;
    internal DbSet<T> dbSet;

    public Repository(AppDBContext db, ITenant tenant)
    {
        _db = db;
        vendorId = tenant.VendorId;
        appType = tenant.AppType;
        this.dbSet = _db.Set<T>();
    }

    public async Task<T> AddAsync(T entity)
    {
        if (typeof(IVendorEntity).IsAssignableFrom(typeof(T)))
        {
            ((IVendorEntity)entity).VendorId = vendorId;
        }


        if (typeof(IAppTypeEntity).IsAssignableFrom(typeof(T)))
        {
            ((IAppTypeEntity)entity).AppType = appType;
        }


        await dbSet.AddAsync(entity);
        return entity;
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = dbSet.AsNoTracking();
        if (typeof(IVendorEntity).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(x => ((IVendorEntity)x).VendorId == vendorId);
        }

        if (typeof(IAppTypeEntity).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(x => ((IAppTypeEntity)x).AppType == appType);
        }

        return await query.AnyAsync(filter, cancellationToken);
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = false)
    {
        IQueryable<T> query;

        query = tracked ? dbSet : dbSet.AsNoTracking();
        query = query.Where(filter);

        if (typeof(IVendorEntity).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(x => ((IVendorEntity)x).VendorId == vendorId);
        }

        if (typeof(IAppTypeEntity).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(x => ((IAppTypeEntity)x).AppType == appType);
        }


        if (!string.IsNullOrEmpty(includeProperties))
        {
            foreach (var includeProp in includeProperties
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProp);
            }
        }


        return await query.FirstOrDefaultAsync();

    }

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter, string? includeProperties = null)
    {
        IQueryable<T> query = dbSet.AsNoTracking();


        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (typeof(IVendorEntity).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(x => ((IVendorEntity)x).VendorId == vendorId);
        }

        if (typeof(IAppTypeEntity).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(x => ((IAppTypeEntity)x).AppType == appType);
        }


        if (!string.IsNullOrEmpty(includeProperties))
        {
            foreach (var includeProp in includeProperties
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProp);
            }
        }

        return await query.ToListAsync();
    }


    public void Remove(T entity)
    {
        dbSet.Remove(entity);
    }

    public void RemoveRange(IList<T> entity)
    {
        dbSet.RemoveRange(entity);
    }



    public async Task<int> CountAsync(Expression<Func<T, bool>> filter)
    {
        IQueryable<T> query;

        query = dbSet.AsNoTracking();
        query = query.Where(filter);

        if (typeof(IVendorEntity).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(x => ((IVendorEntity)x).VendorId == vendorId);
        }

        if (typeof(IAppTypeEntity).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(x => ((IAppTypeEntity)x).AppType == appType);
        }


        return await query.CountAsync();

    }
}
