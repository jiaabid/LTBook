import { NavLink, Navigate, Route, Routes } from 'react-router'
import { useAuth } from './auth/useAuth'
import { AdminPage } from './pages/AdminPage'
import { LoginPage } from './pages/LoginPage'
import { MyBookingsPage } from './pages/MyBookingsPage'
import { ToolPage } from './pages/ToolPage'
import { ToolsPage } from './pages/ToolsPage'

export default function App() {
  const { user, isAdmin, isLoading, logout } = useAuth()

  if (isLoading) return <p className="muted center">Loading…</p>
  if (!user) return <LoginPage />

  return (
    <>
      <header className="topbar">
        <span className="brand">ToolBook</span>
        <nav>
          <NavLink to="/" end>
            Tools
          </NavLink>
          <NavLink to="/bookings">My bookings</NavLink>
          {isAdmin && <NavLink to="/admin">Admin</NavLink>}
        </nav>
        <span className="user">
          {user.displayName}
          <button className="link" onClick={logout}>
            Sign out
          </button>
        </span>
      </header>
      <Routes>
        <Route path="/" element={<ToolsPage />} />
        <Route path="/tools/:toolId" element={<ToolPage />} />
        <Route path="/bookings" element={<MyBookingsPage />} />
        {isAdmin && <Route path="/admin" element={<AdminPage />} />}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  )
}
