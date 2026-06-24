import { Outlet } from 'react-router-dom'
import { AdminNav } from '@/widgets/AdminNav'

export function AdminLayout() {
  return (
    <div className="flex min-h-screen bg-background">
      <AdminNav />
      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
    </div>
  )
}
