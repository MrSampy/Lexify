import { Component, type ErrorInfo, type ReactNode } from 'react'
import i18n from '@/shared/config/i18n'

interface Props {
  children: ReactNode
}

interface State {
  error: Error | null
}

/**
 * Last-resort boundary: an unhandled render error shows a recoverable screen
 * instead of crashing the whole app to a blank page.
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled render error:', error, info.componentStack)
  }

  handleReload = () => {
    this.setState({ error: null })
    window.location.assign('/')
  }

  render() {
    if (!this.state.error) return this.props.children

    return (
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          gap: 16,
          padding: 24,
          textAlign: 'center',
        }}
      >
        <span style={{ fontSize: 40 }}>😵</span>
        <h1 className="ds-h2" style={{ margin: 0 }}>
          {i18n.t('errorBoundary.title')}
        </h1>
        <p className="ds-body" style={{ color: 'var(--fg-3)', margin: 0 }}>
          {i18n.t('errorBoundary.description')}
        </p>
        <button className="lx-btn-primary" onClick={this.handleReload}>
          {i18n.t('errorBoundary.back')}
        </button>
      </div>
    )
  }
}
