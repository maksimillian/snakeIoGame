import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useLocation, useNavigate } from 'react-router-dom';
import styled, { createGlobalStyle, ThemeProvider } from 'styled-components';
import InfoPage from './pages/InfoPage';
import PrivacyPolicy from './pages/PrivacyPolicy';
import Updates from './components/News/Updates';
import PlayPage from './pages/PlayPage';
import GamePage from './pages/GamePage';

// Theme
const theme = {
  colors: {
    primary: '#4CAF50',
    secondary: '#2196F3',
    background: '#0A0A0A',
    surface: '#1A1A1A',
    surfaceHover: '#2A2A2A',
    text: '#FFFFFF',
    textSecondary: '#B0B0B0',
    success: '#4CAF50',
    error: '#F44336',
    warning: '#FFC107',
    info: '#2196F3',
    border: 'rgba(255, 255, 255, 0.1)',
    gradient: 'linear-gradient(135deg, #4CAF50 0%, #2196F3 100%)'
  },
  spacing: {
    xsmall: '4px',
    small: '8px',
    medium: '16px',
    large: '24px',
    xlarge: '32px',
    xxlarge: '48px'
  },
  fontSizes: {
    small: '14px',
    medium: '16px',
    large: '20px',
    xlarge: '24px',
    xxlarge: '32px',
    xxxlarge: '48px'
  },
  shadows: {
    small: '0 2px 4px rgba(0, 0, 0, 0.2)',
    medium: '0 4px 8px rgba(0, 0, 0, 0.2)',
    large: '0 8px 16px rgba(0, 0, 0, 0.2)'
  }
};

// Global Styles
const GlobalStyle = createGlobalStyle`
  * {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
  }

  body {
    font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen',
      'Ubuntu', 'Cantarell', 'Fira Sans', 'Droid Sans', 'Helvetica Neue',
      sans-serif;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
    background-color: ${props => props.theme.colors.background};
    color: ${props => props.theme.colors.text};
    line-height: 1.6;
  }

  a {
    color: ${props => props.theme.colors.primary};
    text-decoration: none;
    transition: all 0.3s ease;
    
    &:hover {
      color: ${props => props.theme.colors.secondary};
    }
  }

  h1, h2, h3, h4, h5, h6 {
    font-weight: 600;
    line-height: 1.2;
    margin-bottom: ${props => props.theme.spacing.medium};
  }
`;

// Layout Components
const Header = styled.header`
  background-color: ${props => props.theme.colors.surface};
  padding: ${props => props.theme.spacing.medium};
  box-shadow: ${props => props.theme.shadows.medium};
  position: sticky;
  top: 0;
  z-index: 100;
  backdrop-filter: blur(10px);
  background-color: rgba(26, 26, 26, 0.8);
`;

const Nav = styled.nav`
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
`;

const Logo = styled(Link)`
  font-size: ${props => props.theme.fontSizes.large};
  font-weight: 700;
  color: ${props => props.theme.colors.text};
  text-decoration: none;
  background: ${props => props.theme.colors.gradient};
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  transition: transform 0.3s ease;

  &:hover {
    transform: scale(1.05);
  }
`;

const NavLinks = styled.div`
  display: flex;
  gap: ${props => props.theme.spacing.large};
`;

const NavLink = styled(Link)`
  color: ${props => props.theme.colors.textSecondary};
  font-weight: 500;
  padding: ${props => props.theme.spacing.small} ${props => props.theme.spacing.medium};
  border-radius: ${props => props.theme.spacing.small};
  transition: all 0.3s ease;

  &:hover {
    color: ${props => props.theme.colors.text};
    background-color: ${props => props.theme.colors.surfaceHover};
  }

  &.active {
    color: ${props => props.theme.colors.primary};
    background-color: ${props => props.theme.colors.surfaceHover};
  }
`;

const PlayButton = styled(Link)`
  background: ${props => props.theme.colors.gradient};
  color: white;
  padding: ${props => props.theme.spacing.small} ${props => props.theme.spacing.large};
  border-radius: ${props => props.theme.spacing.small};
  font-weight: 600;
  transition: all 0.3s ease;
  box-shadow: ${props => props.theme.shadows.small};

  &:hover {
    transform: translateY(-2px);
    box-shadow: ${props => props.theme.shadows.medium};
  }
`;

const Main = styled.main`
  max-width: 1200px;
  margin: 0 auto;
  padding: ${props => props.theme.spacing.large};
  flex: 1;
`;

const Footer = styled.footer`
  background: linear-gradient(90deg, #181818 0%, #23272f 100%);
  padding: ${props => props.theme.spacing.xxlarge} ${props => props.theme.spacing.large} ${props => props.theme.spacing.large};
  margin-top: auto;
  text-align: center;
  color: ${props => props.theme.colors.textSecondary};
  border-top: 1px solid ${props => props.theme.colors.border};
  box-shadow: 0 -2px 16px rgba(0,0,0,0.15);
`;

const FooterContent = styled.div`
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: ${props => props.theme.spacing.xlarge};
  text-align: left;
`;

const FooterSection = styled.div`
  display: flex;
  flex-direction: column;
  gap: ${props => props.theme.spacing.medium};
  min-width: 180px;
`;

const FooterTitle = styled.h4`
  color: ${props => props.theme.colors.text};
  font-size: ${props => props.theme.fontSizes.medium};
  margin-bottom: ${props => props.theme.spacing.small};
  letter-spacing: 1px;
`;

const FooterLink = styled(Link)`
  color: ${props => props.theme.colors.textSecondary};
  font-size: ${props => props.theme.fontSizes.small};
  transition: color 0.3s ease;
  margin-bottom: 4px;
  background: none;
  border: none;
  padding: 0;
  font: inherit;
  cursor: pointer;
  text-align: left;
  text-decoration: none;
  display: inline;

  &:hover {
    color: ${props => props.theme.colors.primary};
    text-decoration: underline;
  }

  &:focus {
    outline: none;
    color: ${props => props.theme.colors.primary};
    text-decoration: underline;
  }
`;

const Copyright = styled.p`
  margin-top: ${props => props.theme.spacing.xlarge};
  font-size: ${props => props.theme.fontSizes.small};
  color: ${props => props.theme.colors.textSecondary};
  text-align: center;
  letter-spacing: 1px;
`;

const AppContainer = styled.div`
  min-height: 100vh;
  display: flex;
  flex-direction: column;
`;

function Navigation() {
  const location = useLocation();
  const navigate = useNavigate();

  const handleInfoClick = (e) => {
    if (location.pathname === '/info') {
      e.preventDefault();
      // If we're already on the info page, scroll to top
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  return (
    <Header>
      <Nav>
        <Logo to="/game">Snake.io</Logo>
        <NavLinks>
          <NavLink 
            to="/info" 
            className={location.pathname === '/info' ? 'active' : ''}
            onClick={handleInfoClick}
          >
            Info
          </NavLink>
          <PlayButton to="/game">Play Now</PlayButton>
        </NavLinks>
      </Nav>
    </Header>
  );
}

function AppLayout() {
  const [infoSectionRefs, setInfoSectionRefs] = React.useState(null);
  const [pendingScrollSection, setPendingScrollSection] = React.useState(null);
  const navigate = useNavigate();
  const location = useLocation();

  // Helper to handle footer anchor clicks
  const handleFooterAnchor = (section) => {
    if (location.pathname === '/info' && infoSectionRefs && infoSectionRefs[section]) {
      infoSectionRefs.scrollToSection(infoSectionRefs[section]);
    } else {
      setPendingScrollSection(section);
      navigate('/info');
    }
  };

  return (
    <AppContainer>
      <Navigation />
      <Routes>
        <Route path="/game" element={<PlayPage />} />
        <Route path="/play" element={<GamePage />} />
        <Route path="/info" element={<InfoPage setSectionRefs={setInfoSectionRefs} pendingScrollSection={pendingScrollSection} setPendingScrollSection={setPendingScrollSection} />} />
        <Route path="/privacy" element={<PrivacyPolicy />} />
        <Route path="/" element={<PlayPage />} />
      </Routes>
      <Footer>
        <FooterContent>
          <FooterSection>
            <FooterTitle>Game</FooterTitle>
            <FooterLink as="button" onClick={() => handleFooterAnchor('howToPlayRef')}>How to Play</FooterLink>
            <FooterLink as="button" onClick={() => handleFooterAnchor('plannedUpdateRef')}>Planned Updates</FooterLink>
            <FooterLink as="button" onClick={() => handleFooterAnchor('updatesRef')}>Updates</FooterLink>
            <FooterLink as="button" onClick={() => handleFooterAnchor('gameFeaturesRef')}>Game Features</FooterLink>
          </FooterSection>
          <FooterSection>
            <FooterTitle>Support</FooterTitle>
            <FooterLink as="button" onClick={() => handleFooterAnchor('termsRef')}>Terms Of Service</FooterLink>
            <FooterLink as="button" onClick={() => handleFooterAnchor('faqRef')}>FAQ</FooterLink>
            <FooterLink as="button" onClick={() => handleFooterAnchor('supportRef')}>Support & Contacts</FooterLink>
            <FooterLink to="/privacy">Privacy Policy</FooterLink>
          </FooterSection>
          <FooterSection>
            <FooterTitle>Community</FooterTitle>
            <FooterLink href="https://discord.gg/snakeio" target="_blank" rel="noopener noreferrer">
              Discord
            </FooterLink>
            <FooterLink href="https://twitter.com/snakeio" target="_blank" rel="noopener noreferrer">
              Twitter
            </FooterLink>
          </FooterSection>
        </FooterContent>
        <Copyright>
          © {new Date().getFullYear()} Snake.io. All rights reserved.
        </Copyright>
      </Footer>
    </AppContainer>
  );
}

function App() {
  return (
    <ThemeProvider theme={theme}>
      <GlobalStyle />
      <Router>
        <AppLayout />
      </Router>
    </ThemeProvider>
  );
}

export default App;
