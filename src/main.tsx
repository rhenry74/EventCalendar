import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import type { ThemeOptions } from '@mui/material/styles'
import { createTheme, ThemeProvider } from '@mui/material/styles'
import './index.css'
import App from './App.tsx'

const themeOptions: ThemeOptions = {
  palette: {
    mode: "dark",
    primary: {
      main: "#60a5fa", // Blue accent color matching calendar
    },
    background: {
      default: "#1f2937", // Dark gray background
    },
    text: {
      primary: "#e5e7eb", // Light text for primary content
      secondary: "#9ca3af", // Gray text for secondary content
    },
    divider: "#374151", // Divider color
    action: {
      hover: "rgba(96, 165, 250, 0.08)", // Subtle blue hover effect
      selected: "rgba(96, 165, 250, 0.15)", // Selected state background
    },
  },
  components: {
    MuiOutlinedInput: {
      styleOverrides: {
        root: ({ theme }) => ({
          "& .MuiOutlinedInput-notchedOutline": {
            borderColor: theme.palette.divider,
          },
          "&:hover .MuiOutlinedInput-notchedOutline": {
            borderColor: theme.palette.primary.main,
          },
          "&.Mui-focused .MuiOutlinedInput-notchedOutline": {
            borderColor: theme.palette.primary.main,
          },
        }),
        input: ({ theme }) => ({
          color: theme.palette.text.primary,
        }),
      },
    },
    MuiInputLabel: {
      styleOverrides: {
        root: ({ theme }) => ({
          color: theme.palette.text.secondary,
        }),
        shrink: ({ theme }) => ({
          color: theme.palette.primary.main,
        }),
      },
    },
  },
}

const theme = createTheme(themeOptions);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider theme={theme}>
      <App />
    </ThemeProvider>
  </StrictMode>,
)
