import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { BranchesPage } from './BranchesPage'
import { useBranches } from '../hooks/useBranches'
import { useAuth } from '../../auth/providers/AuthProvider'
import { useToast } from '../../../components/ui/ToastProvider'
import { useNavigate } from 'react-router-dom'

vi.mock('../hooks/useBranches', () => ({ useBranches: vi.fn() }))
vi.mock('../../auth/providers/AuthProvider', () => ({ useAuth: vi.fn() }))
vi.mock('../../../components/ui/ToastProvider', () => ({ useToast: vi.fn() }))
vi.mock('react-router-dom', () => ({ useNavigate: vi.fn() }))
vi.mock('./CreateBranchDialog', () => ({ CreateBranchDialog: () => <div role="dialog">Create branch dialog</div> }))
vi.mock('../../../components/ui/ConfirmationDialog', () => ({ ConfirmationDialog: () => <div role="alertdialog">Confirmation dialog</div> }))

const branch = {
  branchId: 7,
  code: 'CT01',
  name: 'Cape Town Central',
  addressLine1: '1 Long Street',
  city: 'Cape Town',
  province: 'Western Cape',
  postalCode: '8001',
  status: 'Active' as const,
  acceptsBookings: true,
  createdAtUtc: '2025-01-01T00:00:00Z',
}

const baseHook = () => ({
  branches: [branch],
  pageNumber: 1,
  pageSize: 5,
  searchTerm: '',
  totalCount: 1,
  totalPages: 1,
  isLoading: false,
  error: null as string | null,
  refresh: vi.fn(),
  addBranch: vi.fn(),
  editBranch: vi.fn(),
  disableBranch: vi.fn(),
  goToPage: vi.fn(),
  updatePageSize: vi.fn(),
  updateSearchTerm: vi.fn(),
})

const mockedUseBranches = vi.mocked(useBranches)
const mockedUseAuth = vi.mocked(useAuth)
const mockedUseToast = vi.mocked(useToast)
const navigate = vi.fn()

beforeEach(() => {
  vi.clearAllMocks()
  mockedUseBranches.mockReturnValue(baseHook())
  mockedUseAuth.mockReturnValue({
    user: null,
    isAuthenticated: true,
    isLoading: false,
    hasRole: vi.fn().mockReturnValue(true),
    signIn: vi.fn(),
    signOut: vi.fn(),
  })
  mockedUseToast.mockReturnValue({ showToast: vi.fn() })
  vi.mocked(useNavigate).mockReturnValue(navigate)
})

describe('BranchesPage', () => {
  it('renders branch details and booking status', () => {
    render(<BranchesPage />)

    expect(screen.getByRole('heading', { name: 'Your branches' })).toBeInTheDocument()
    expect(screen.getByText('Cape Town Central')).toBeInTheDocument()
    expect(screen.getByText('CT01')).toBeInTheDocument()
    expect(screen.getByText('Accepting bookings')).toBeInTheDocument()
    expect(screen.getByText('1 locations in your network')).toBeInTheDocument()
  })

  it('passes search, page-size, pagination, and edit interactions to the hook and router', () => {
    const hook = baseHook()
    hook.pageNumber = 2
    hook.totalPages = 3
    mockedUseBranches.mockReturnValue(hook)
    render(<BranchesPage />)

    fireEvent.change(screen.getByRole('textbox', { name: 'Search branches' }), { target: { value: 'Cape' } })
    fireEvent.change(screen.getByRole('combobox', { name: 'Branches per page' }), { target: { value: '20' } })
    fireEvent.click(screen.getByRole('button', { name: 'Previous page' }))
    fireEvent.click(screen.getByRole('button', { name: 'Next page' }))
    fireEvent.click(screen.getByRole('button', { name: 'Edit Cape Town Central' }))

    expect(hook.updateSearchTerm).toHaveBeenCalledWith('Cape')
    expect(hook.updatePageSize).toHaveBeenCalledWith(20)
    expect(hook.goToPage).toHaveBeenNthCalledWith(1, 1)
    expect(hook.goToPage).toHaveBeenNthCalledWith(2, 3)
    expect(navigate).toHaveBeenCalledWith('/branches/7/configuration', { state: { branch } })
  })

  it('shows loading, error retry, and empty states', () => {
    const hook = baseHook()
    hook.isLoading = true
    mockedUseBranches.mockReturnValue(hook)
    const { rerender } = render(<BranchesPage />)
    expect(document.querySelectorAll('.skeleton-card')).toHaveLength(3)

    hook.isLoading = false
    hook.error = 'Could not load branches'
    rerender(<BranchesPage />)
    expect(screen.getByText('Could not load branches')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(hook.refresh).toHaveBeenCalledOnce()

    hook.error = null
    hook.branches = []
    hook.totalCount = 0
    rerender(<BranchesPage />)
    expect(screen.getByRole('heading', { name: 'No branches found' })).toBeInTheDocument()
  })

  it('only shows Add branch to administrators', () => {
    mockedUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: true,
      isLoading: false,
      hasRole: vi.fn().mockReturnValue(false),
      signIn: vi.fn(),
      signOut: vi.fn(),
    })

    render(<BranchesPage />)

    expect(screen.queryByRole('button', { name: 'Add branch' })).not.toBeInTheDocument()
  })
})
