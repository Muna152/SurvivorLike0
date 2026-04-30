using UnityEngine;

/// <summary>
/// Orbital weapon that spawns and manages orbiting objects around the player.
/// Orbiting objects rotate around the player and damage enemies on contact.
/// </summary>
public class OrbitalWeapon : WeaponBase
{
    [SerializeField] private GameObject _orbitalPrefab;
    private OrbitalObject[] _orbitals;
    private int _orbitalCount;
    private float _orbitalRadius;
    private float _rotationSpeed;
    private float _currentAngle;
    private float _damage;
    private string _poolKey;

    protected override void Attack()
    {
        // Orbital weapons don't "attack" in the traditional sense.
        // Orbiting objects are always active and damage enemies on contact.
        // This method is called for cooldown management only.
    }

    public override void Initialize(WeaponData data, PlayerStats stats)
    {
        base.Initialize(data, stats);

        // Use projectilePrefab from WeaponData when created at runtime
        if (_orbitalPrefab == null && data != null && data.projectilePrefab != null)
        {
            _orbitalPrefab = data.projectilePrefab;
        }

        // Cache pool key
        if (_orbitalPrefab != null)
        {
            _poolKey = _orbitalPrefab.name.Replace("(Clone)", "").Trim();
        }

        if (data != null)
        {
            var ld = CurrentLevelData;
            if (ld != null)
            {
                _orbitalCount = (int)ld.projectileCount;
                _orbitalRadius = 2.5f;
                _rotationSpeed = 120f; // degrees per second
                _damage = ld.damage * stats.DamageMultiplier;
            }
        }

        CreateOrbitals();
        UpdateOrbitalProperties();
    }

    public override void Upgrade()
    {
        base.Upgrade();
        
        var ld = CurrentLevelData;
        if (ld != null && _playerStats != null)
        {
            _orbitalCount = (int)ld.projectileCount;
            _damage = ld.damage * _playerStats.DamageMultiplier;
        }

        RecreateOrbitals();
        UpdateOrbitalProperties();
    }

    public override void OnPlayerMoved(Vector2 position, Vector2 direction)
    {
        base.OnPlayerMoved(position, direction);
        
        // Update orbital center position
        if (_orbitals != null)
        {
            foreach (var orbital in _orbitals)
            {
                if (orbital != null)
                {
                    orbital.SetCenterPosition(position);
                }
            }
        }
    }

    private void CreateOrbitals()
    {
        // Clear existing orbitals
        if (_orbitals != null)
        {
            foreach (var orbital in _orbitals)
            {
                if (orbital != null)
                {
                    if (PoolManager.Instance != null && PoolManager.Instance.HasPool(_poolKey))
                        PoolManager.Instance.Return(_poolKey, orbital);
                    else
                        Destroy(orbital.gameObject);
                }
            }
        }

        // Register pool if not already registered
        if (PoolManager.Instance != null && !PoolManager.Instance.HasPool(_poolKey) && _orbitalPrefab != null)
        {
            PoolManager.Instance.Register<OrbitalObject>(
                _poolKey,
                () =>
                {
                    GameObject obj = Instantiate(_orbitalPrefab, transform);
                    var orb = obj.GetComponent<OrbitalObject>();
                    if (orb == null) orb = obj.AddComponent<OrbitalObject>();
                    return orb;
                },
                orb => orb.gameObject.SetActive(false)
            );
        }

        _orbitals = new OrbitalObject[_orbitalCount];

        for (int i = 0; i < _orbitalCount; i++)
        {
            OrbitalObject orbital = null;
            if (PoolManager.Instance != null && PoolManager.Instance.HasPool(_poolKey))
                orbital = PoolManager.Instance.Get<OrbitalObject>(_poolKey);

            if (orbital == null)
            {
                GameObject orbitalObj = Instantiate(_orbitalPrefab, transform);
                orbital = orbitalObj.GetComponent<OrbitalObject>();
                if (orbital == null)
                    orbital = orbitalObj.AddComponent<OrbitalObject>();
            }

            _orbitals[i] = orbital;
            _orbitals[i].Initialize(_playerPosition, _orbitalRadius, _damage);
        }
    }

    private void RecreateOrbitals()
    {
        if (_orbitals == null || _orbitals.Length != _orbitalCount)
        {
            CreateOrbitals();
        }
        else
        {
            UpdateOrbitalProperties();
        }
    }

    private void UpdateOrbitalProperties()
    {
        if (_orbitals == null) return;

        float angleStep = 360f / _orbitalCount;
        
        for (int i = 0; i < _orbitals.Length; i++)
        {
            if (_orbitals[i] != null)
            {
                float angle = _currentAngle + (angleStep * i);
                _orbitals[i].UpdateProperties(_orbitalRadius, _rotationSpeed, _damage);
                _orbitals[i].SetAngle(angle);
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        // Update rotation angle
        _currentAngle += _rotationSpeed * Time.deltaTime;
        if (_currentAngle >= 360f)
        {
            _currentAngle -= 360f;
        }

        // Update orbital angles
        if (_orbitals != null)
        {
            float angleStep = 360f / _orbitals.Length;

            for (int i = 0; i < _orbitals.Length; i++)
            {
                if (_orbitals[i] != null)
                {
                    float angle = _currentAngle + (angleStep * i);
                    _orbitals[i].SetAngle(angle);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up orbitals
        if (_orbitals != null)
        {
            foreach (var orbital in _orbitals)
            {
                if (orbital != null)
                {
                    PoolManager.Instance?.Return(_poolKey, orbital);
                }
            }
            _orbitals = null;
        }
    }
}
