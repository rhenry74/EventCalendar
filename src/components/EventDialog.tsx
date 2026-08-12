import React, { useState, useEffect } from 'react';
import { 
  Dialog, 
  DialogTitle, 
  DialogContent, 
  DialogActions, 
  Button, 
  TextField,
  Box
} from '@mui/material';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import type { Event, PartialEvent } from '../types';
import type { Theme } from '@mui/material/styles';

interface EventDialogProps {
  open: boolean;
  onClose: () => void;
  initialEvent?: Event;
  onSave: (event: PartialEvent) => void;
  theme: Theme;
}

const EventDialog: React.FC<EventDialogProps> = ({ open, onClose, initialEvent, onSave, theme }) => {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [location, setLocation] = useState('');
  const [category, setCategory] = useState('General');
  const [date, setDate] = useState<Date | null>(null);

  useEffect(() => {
    if (initialEvent) {
      setTitle(initialEvent.title);
      setDescription(initialEvent.description || '');
      setLocation(initialEvent.location || '');
      setCategory(initialEvent.category || 'General');
      setDate(new Date(initialEvent.date));
    } else {
      setTitle('');
      setDescription('');
      setLocation('');
      setCategory('General');
      setDate(new Date());
    }
  }, [initialEvent]);

  useEffect(() => {
    if (!open && !initialEvent) {
      setTitle('');
      setDescription('');
      setLocation('');
      setCategory('General');
      setDate(new Date());
    }
  }, [open]);

  const handleSave = () => {
    if (!title.trim()) {
      alert("Title is required");
      return;
    }
    
    const eventData: PartialEvent = {
      id: initialEvent?.id || '',
      title: title.trim(),
      description: description.trim(),
      location: location.trim(),
      category,
      date: date?.toISOString() || new Date().toISOString(),
    };
    
    onSave(eventData);
    onClose();
  };

  const handleDateChange = (newValue: Date | null) => {
    setDate(newValue || new Date());
  };

  if (!open) return null;

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Dialog 
        open={open} 
        onClose={onClose} 
        fullWidth 
        maxWidth="sm"
        sx={{
          backgroundColor: theme.palette.background.default,
          borderRadius: 2,
          border: `1px solid ${theme.palette.divider}`,
          boxShadow: theme.shadows[3],
          '& .MuiDialog-paper': {
            backgroundColor: theme.palette.background.default,
            boxShadow: theme.shadows[3]
          }
        }}
      >
        <DialogTitle 
          sx={{ 
            color: theme.palette.text.primary,
            borderBottom: `1px solid ${theme.palette.divider}`
          }}
        >
          {initialEvent ? 'Edit Event' : 'Add New Event'}
        </DialogTitle>
        <DialogContent sx={{ color: theme.palette.text.secondary }}>
          <Box 
            sx={{ 
              display: 'flex', 
              flexDirection: 'column', 
              gap: 2, 
              mt: 1
            }}
          >
            <TextField
              label="Title"
              fullWidth
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              onKeyDown={(e) => e.key === 'Enter' && handleSave()}
              autoFocus
              variant="outlined"
              sx={{ 
                '& .MuiOutlinedInput-root': {
                  borderRadius: 1,
                  '& fieldset': { borderColor: theme.palette.divider },
                  '&:hover fieldset': { borderColor: theme.palette.primary.main },
                  '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                }
              }}
            />
            <TextField
              label="Description"
              fullWidth
              multiline
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              variant="outlined"
              sx={{ 
                '& .MuiOutlinedInput-root': {
                  borderRadius: 1,
                  '& fieldset': { borderColor: theme.palette.divider },
                  '&:hover fieldset': { borderColor: theme.palette.primary.main },
                  '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                }
              }}
            />
            <TextField
              label="Location"
              fullWidth
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              variant="outlined"
              sx={{ 
                '& .MuiOutlinedInput-root': {
                  borderRadius: 1,
                  '& fieldset': { borderColor: theme.palette.divider },
                  '&:hover fieldset': { borderColor: theme.palette.primary.main },
                  '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                }
              }}
            />
            <TextField
              label="Category"
              fullWidth
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              variant="outlined"
              sx={{ 
                '& .MuiOutlinedInput-root': {
                  borderRadius: 1,
                  '& fieldset': { borderColor: theme.palette.divider },
                  '&:hover fieldset': { borderColor: theme.palette.primary.main },
                  '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                }
              }}
            />
            <DatePicker
              label="Date & Time"
              value={date}
              onChange={handleDateChange}
              slotProps={{
                textField: {
                  fullWidth: true,
                  variant: 'outlined',
                  sx: {
                    '& .MuiOutlinedInput-root': {
                      borderRadius: 1,
                      '& fieldset': { borderColor: theme.palette.divider },
                      '&:hover fieldset': { borderColor: theme.palette.primary.main },
                      '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                    }
                  }
                }
              }}
            />
          </Box>
        </DialogContent>
        <DialogActions sx={{ 
          justifyContent: 'space-between',
          px: 2 
        }}>
          <Button 
            onClick={onClose} 
            sx={{ 
              color: theme.palette.text.primary,
              '&:hover': { backgroundColor: theme.palette.action.hover, color: theme.palette.text.primary }
            }}
          >Cancel</Button>
          <Button onClick={handleSave} variant="contained" sx={{ backgroundColor: theme.palette.primary.main }}>Save</Button>
        </DialogActions>
      </Dialog>
    </LocalizationProvider>
  );
};

export default EventDialog;
