import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import LoginView from '../views/LoginView.vue'
import FelvasarlasView from '../views/FelvasarlasView.vue'
import EladasView from '../views/EladasView.vue'
import EgyenlegView from '../views/EgyenlegView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
    { path: '/', name: 'felvasarlas', component: FelvasarlasView, meta: { admin: true } },
    { path: '/eladas', name: 'eladas', component: EladasView, meta: { admin: true } },
    { path: '/egyenleg', name: 'egyenleg', component: EgyenlegView }
  ]
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.token) return { name: 'login' }
  if (to.name === 'login' && auth.token) return { name: auth.role === 'Admin' ? 'felvasarlas' : 'egyenleg' }
  if (to.meta.admin && auth.role !== 'Admin') return { name: 'egyenleg' }
})

export default router
