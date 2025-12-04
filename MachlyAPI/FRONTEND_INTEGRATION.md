# Integración Frontend - Endpoints de Gestión de Roles

Este documento describe cómo usar los nuevos endpoints de gestión de roles desde el frontend.

## Endpoint 1: Actualizar Rol de Usuario (RENTER → PROVIDER)

**URL:** `PUT /api/users/{id}/role`

**Propósito:** Permitir que un RENTER actualice su propio rol a PROVIDER al crear su primera máquina.

**Autenticación:** Requiere token JWT (usuario autenticado).

**Autorización:** Solo el mismo usuario puede cambiar su rol.

### Request

```javascript
// Ejemplo en JavaScript/TypeScript
const updateUserRole = async (userId, token) => {
  const response = await fetch(`https://tu-api.com/api/users/${userId}/role`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      role: 1  // 0=ADMIN, 1=PROVIDER, 2=RENTER
    })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
  
  return await response.json();
};

// Uso: Cuando un RENTER crea su primera máquina
try {
  const updatedUser = await updateUserRole(currentUserId, authToken);
  console.log('Usuario actualizado a PROVIDER:', updatedUser);
} catch (error) {
  console.error('Error al actualizar rol:', error.message);
}
```

### Response (200 OK)

```json
{
  "id": "507f1f77bcf86cd799439011",
  "name": "Juan",
  "lastname": "Pérez",
  "email": "juan@example.com",
  "phone": "+591 12345678",
  "role": 1,
  "photoUrl": "https://example.com/photo.jpg",
  "verified": true,
  "createdAt": "2025-12-04T19:26:33.284Z"
}
```

### Validaciones

- ✅ Solo permite cambiar de RENTER (2) a PROVIDER (1)
- ❌ No permite cambiar a ADMIN (0)
- ❌ No permite cambiar de PROVIDER a RENTER
- ❌ El id debe coincidir con el usuario autenticado

### Errores Posibles

```json
// 403 Forbidden - Intentando cambiar el rol de otro usuario
{
  "message": "Forbidden"
}

// 400 Bad Request - Intentando cambiar a ADMIN
{
  "message": "Cannot change role to ADMIN"
}

// 400 Bad Request - Intentando cambiar de PROVIDER a RENTER
{
  "message": "Cannot change role from PROVIDER to RENTER"
}

// 400 Bad Request - Usuario ya es PROVIDER
{
  "message": "User is already a PROVIDER"
}
```

---

## Endpoint 2: Actualizar Rol de Usuario (Admin)

**URL:** `PUT /api/admin/users/{id}/role`

**Propósito:** Permitir que un ADMIN cambie el rol de cualquier usuario.

**Autenticación:** Requiere token JWT.

**Autorización:** Solo ADMIN (rol 0).

### Request

```javascript
// Ejemplo en JavaScript/TypeScript
const updateUserRoleAsAdmin = async (userId, newRole, adminToken) => {
  const response = await fetch(`https://tu-api.com/api/admin/users/${userId}/role`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${adminToken}`
    },
    body: JSON.stringify({
      role: newRole  // 0=ADMIN, 1=PROVIDER, 2=RENTER
    })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
  
  return await response.json();
};

// Uso: Panel de administración
try {
  const updatedUser = await updateUserRoleAsAdmin('507f1f77bcf86cd799439011', 1, adminToken);
  console.log('Rol actualizado por admin:', updatedUser);
} catch (error) {
  console.error('Error al actualizar rol:', error.message);
}
```

### Response (200 OK)

```json
{
  "id": "507f1f77bcf86cd799439011",
  "name": "María",
  "lastname": "González",
  "email": "maria@example.com",
  "phone": "+591 87654321",
  "role": 1,
  "photoUrl": "https://example.com/photo2.jpg",
  "verified": false,
  "createdAt": "2025-12-04T19:26:33.284Z"
}
```

### Validaciones

- ✅ Permite cambiar a cualquier rol (0, 1, 2)
- ❌ No permite cambiar el rol del último ADMIN

### Errores Posibles

```json
// 401 Unauthorized - Sin token o token inválido
{
  "message": "Unauthorized"
}

// 403 Forbidden - Usuario no es ADMIN
{
  "message": "Forbidden"
}

// 400 Bad Request - Intentando cambiar el rol del último ADMIN
{
  "message": "Cannot change the role of the last admin"
}

// 404 Not Found - Usuario no existe
{
  "message": "User not found"
}
```

---

## Ejemplo de Integración Completa

### React/Next.js

```typescript
// services/userService.ts
export const userService = {
  // Endpoint 1: Usuario actualiza su propio rol
  upgradeToProvider: async (userId: string) => {
    const token = localStorage.getItem('authToken');
    const response = await fetch(`/api/users/${userId}/role`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ role: 1 })
    });
    
    if (!response.ok) throw new Error('Failed to upgrade to provider');
    return response.json();
  },

  // Endpoint 2: Admin actualiza rol de usuario
  updateUserRole: async (userId: string, role: number) => {
    const token = localStorage.getItem('authToken');
    const response = await fetch(`/api/admin/users/${userId}/role`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ role })
    });
    
    if (!response.ok) throw new Error('Failed to update user role');
    return response.json();
  }
};

// Componente: Crear primera máquina
const CreateMachineForm = () => {
  const { user, updateUser } = useAuth();
  
  const handleSubmit = async (machineData) => {
    try {
      // Si el usuario es RENTER, actualizarlo a PROVIDER
      if (user.role === 2) {
        const updatedUser = await userService.upgradeToProvider(user.id);
        updateUser(updatedUser);
      }
      
      // Crear la máquina
      await machineService.create(machineData);
      
      toast.success('¡Máquina creada y ahora eres PROVIDER!');
    } catch (error) {
      toast.error(error.message);
    }
  };
  
  return (/* ... */);
};

// Componente: Panel de admin
const AdminUserManagement = () => {
  const handleRoleChange = async (userId: string, newRole: number) => {
    try {
      const updatedUser = await userService.updateUserRole(userId, newRole);
      toast.success('Rol actualizado correctamente');
      refreshUserList();
    } catch (error) {
      toast.error(error.message);
    }
  };
  
  return (/* ... */);
};
```

---

## Roles Disponibles

| Valor | Rol      | Descripción                                    |
|-------|----------|------------------------------------------------|
| 0     | ADMIN    | Administrador con acceso completo              |
| 1     | PROVIDER | Proveedor que puede crear y gestionar máquinas |
| 2     | RENTER   | Usuario que puede rentar máquinas              |

---

## Notas Importantes

1. **Seguridad:** Ambos endpoints requieren autenticación JWT válida.
2. **Flujo RENTER → PROVIDER:** Se recomienda llamar al endpoint 1 automáticamente cuando un RENTER crea su primera máquina.
3. **Protección de ADMIN:** El sistema previene que se elimine o cambie el rol del último ADMIN.
4. **Validación de Tokens:** Asegúrate de incluir el token JWT en el header `Authorization: Bearer {token}`.
5. **Manejo de Errores:** Implementa manejo de errores apropiado para cada caso de uso.
